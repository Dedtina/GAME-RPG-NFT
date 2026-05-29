mergeInto(LibraryManager.library, {
  FirebaseCreateOrLoadUser: function(addressPtr, callbackObjectPtr) {
    const address = UTF8ToString(addressPtr);
    const callbackObjectName = UTF8ToString(callbackObjectPtr);

    if (window.FirebaseCreateOrLoadUser) {
      window.FirebaseCreateOrLoadUser(address, callbackObjectName);
    } else {
      console.error("FirebaseCreateOrLoadUser is not available.");
    }
  },

  FirebaseLoadGameData: function(userIdPtr, callbackObjectPtr) {
    const userId = UTF8ToString(userIdPtr);
    const callbackObjectName = UTF8ToString(callbackObjectPtr);

    if (window.FirebaseLoadGameData) {
      window.FirebaseLoadGameData(userId, callbackObjectName);
    } else {
      console.error("FirebaseLoadGameData is not available.");
    }
  },

  FirebaseSaveGameData: function(userIdPtr, gameDataJsonPtr, callbackObjectPtr) {
    const userId = UTF8ToString(userIdPtr);
    const gameDataJson = UTF8ToString(gameDataJsonPtr);
    const callbackObjectName = UTF8ToString(callbackObjectPtr);

    if (window.FirebaseSaveGameData) {
      window.FirebaseSaveGameData(userId, gameDataJson, callbackObjectName);
    } else {
      console.error("FirebaseSaveGameData is not available.");
    }
  },

  FirebaseDeleteGameData: function(userIdPtr, callbackObjectPtr) {
    const userId = UTF8ToString(userIdPtr);
    const callbackObjectName = UTF8ToString(callbackObjectPtr);

    if (window.FirebaseDeleteGameData) {
      window.FirebaseDeleteGameData(userId, callbackObjectName);
    } else {
      console.error("FirebaseDeleteGameData is not available.");
    }
  },

  FirebaseConvertEquipmentToNFT: function(walletAddressPtr, itemIdPtr, instanceIdPtr, enhanceLevel, itemNamePtr, itemDescriptionPtr, itemImageUriPtr, contractAddressPtr, callbackObjectPtr) {
    const walletAddress = UTF8ToString(walletAddressPtr);
    const itemId = UTF8ToString(itemIdPtr);
    const instanceId = UTF8ToString(instanceIdPtr);
    const itemName = UTF8ToString(itemNamePtr);
    const itemDescription = UTF8ToString(itemDescriptionPtr);
    const itemImageUri = UTF8ToString(itemImageUriPtr);
    const contractAddress = UTF8ToString(contractAddressPtr);
    const callbackObjectName = UTF8ToString(callbackObjectPtr);

    if (window.FirebaseConvertEquipmentToNFT) {
      window.FirebaseConvertEquipmentToNFT(walletAddress, itemId, instanceId, enhanceLevel, itemName, itemDescription, itemImageUri, contractAddress, callbackObjectName);
    } else {
      console.error("FirebaseConvertEquipmentToNFT is not available.");
    }
  }
});
