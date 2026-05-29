require("dotenv").config();

const path = require("path");
const express = require("express");
const cors = require("cors");
const admin = require("firebase-admin");
const { ethers } = require("ethers");

const ERC721_MINT_TO_ABI = [
  "function mintTo(address to, string uri) returns (uint256)"
];

const serviceAccountPath = process.env.SERVICE_ACCOUNT_PATH || "./serviceAccountKey.json";
const serviceAccount = require(path.resolve(__dirname, serviceAccountPath));

admin.initializeApp({
  credential: admin.credential.cert(serviceAccount)
});

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

app.get("/health", (req, res) => {
  res.json({ ok: true });
});

app.post("/convert-equipment-to-nft", async (req, res) => {
  try {
    const data = req.body || {};
    const walletAddress = normalizeAddress(data.walletAddress);
    const itemId = requireString(data.itemId, "itemId");
    const instanceId = requireString(data.instanceId, "instanceId");
    const enhanceLevel = requireEnhanceLevel(data.enhanceLevel);
    const itemName = requireString(data.itemName, "itemName");
    const itemDescription = data.itemDescription || "";
    const itemImageUri = data.itemImageUri || "";
    const contractAddress = normalizeAddress(data.contractAddress);

    const saveRef = admin
      .firestore()
      .doc(`users/${walletAddress.toLowerCase()}/saves/main`);
    const saveSnap = await saveRef.get();

    if (!saveSnap.exists) {
      throw new Error("Player save does not exist.");
    }

    const saveDoc = saveSnap.data() || {};
    const gameData = parseGameData(saveDoc);
    const equipment = getOwnedEquipmentInstance(gameData, itemId, instanceId);

    if (!equipment) {
      throw new Error("Player does not own this equipment item.");
    }

    if (Number(equipment.enhanceLevel || 0) !== enhanceLevel) {
      throw new Error("Equipment enhancement level does not match save data.");
    }

    const metadata = {
      name: itemName,
      description: itemDescription,
      image: itemImageUri,
      attributes: [
        { trait_type: "Game Item ID", value: itemId },
        { trait_type: "Enhance Level", value: enhanceLevel },
        { trait_type: "Source", value: "DATN Unity RPG" }
      ]
    };

    const rpcUrl = getRequiredEnv("RPC_URL");
    const minterPrivateKey = getRequiredEnv("MINTER_PRIVATE_KEY");
    const provider = new ethers.JsonRpcProvider(rpcUrl);
    const wallet = new ethers.Wallet(minterPrivateKey, provider);
    const contract = new ethers.Contract(contractAddress, ERC721_MINT_TO_ABI, wallet);

    const tx = await contract.mintTo(walletAddress, toDataUri(metadata));
    const receipt = await tx.wait();

    removeEquipmentInstance(gameData, instanceId);

    await saveRef.set(
      buildSplitSaveDocument(gameData, {
        lastNftConversion: {
          itemId,
          instanceId,
          enhanceLevel,
          itemName,
          contractAddress,
          owner: walletAddress,
          transactionHash: receipt.hash,
          convertedAt: admin.firestore.FieldValue.serverTimestamp()
        }
      }),
      { merge: true }
    );

    res.json({
      ok: true,
      itemId,
      instanceId,
      enhanceLevel,
      itemName,
      contractAddress,
      owner: walletAddress,
      transactionHash: receipt.hash
    });
  } catch (error) {
    console.error(error);
    res.status(400).json({
      ok: false,
      error: error.message || String(error)
    });
  }
});

const port = Number(process.env.PORT || 8787);
app.listen(port, () => {
  console.log(`Local NFT backend running at http://localhost:${port}`);
});

function requireString(value, fieldName) {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(`${fieldName} is required.`);
  }

  return value.trim();
}

function normalizeAddress(value) {
  const address = requireString(value, "address");

  if (!ethers.isAddress(address)) {
    throw new Error(`Invalid address: ${address}`);
  }

  return ethers.getAddress(address);
}

function requireEnhanceLevel(value) {
  const enhanceLevel = Number(value);

  if (!Number.isInteger(enhanceLevel) || enhanceLevel < 0) {
    throw new Error("enhanceLevel must be a non-negative integer.");
  }

  return enhanceLevel;
}

function parseGameData(saveDoc) {
  if (!saveDoc || typeof saveDoc !== "object") {
    throw new Error("Player save is empty.");
  }

  return normalizeGameData(saveDoc);
}

function normalizeGameData(data) {
  data = data || {};

  return {
    currency: Number(data.currency || 0),
    inventory: normalizeUnityDictionary(data.inventory),
    stash: normalizeUnityDictionary(data.stash),
    equipments: Array.isArray(data.equipments) ? data.equipments : [],
    inventoryEquipments: Array.isArray(data.inventoryEquipments) ? data.inventoryEquipments : [],
    equippedEquipments: Array.isArray(data.equippedEquipments) ? data.equippedEquipments : [],
    skills: normalizeUnityDictionary(data.skills),
    collectedWorldItems: normalizeUnityDictionary(data.collectedWorldItems)
  };
}

function normalizeUnityDictionary(value) {
  if (!value || !Array.isArray(value.keys) || !Array.isArray(value.values)) {
    return {
      keys: [],
      values: []
    };
  }

  return {
    keys: value.keys,
    values: value.values
  };
}

function buildSplitSaveDocument(gameData, extraFields = {}) {
  return {
    ...normalizeGameData(gameData),
    ...extraFields,
    saveSchemaVersion: 2,
    updatedAt: admin.firestore.FieldValue.serverTimestamp()
  };
}

function getOwnedEquipmentInstance(gameData, itemId, instanceId) {
  if (!Array.isArray(gameData.inventoryEquipments)) {
    return null;
  }

  return gameData.inventoryEquipments.find(
    (item) => item && item.itemID === itemId && item.instanceId === instanceId
  );
}

function removeEquipmentInstance(gameData, instanceId) {
  if (!Array.isArray(gameData.inventoryEquipments)) {
    return;
  }

  const index = gameData.inventoryEquipments.findIndex(
    (item) => item && item.instanceId === instanceId
  );

  if (index >= 0) {
    gameData.inventoryEquipments.splice(index, 1);
  }
}

function toDataUri(metadata) {
  const json = JSON.stringify(metadata);
  return `data:application/json;base64,${Buffer.from(json).toString("base64")}`;
}

function getRequiredEnv(name) {
  const value = process.env[name];

  if (!value) {
    throw new Error(`${name} is not configured.`);
  }

  return value;
}
