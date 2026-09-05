using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace RollAndEscape.Monetization
{
    /// <summary>
    /// Unity IAP wrapper for the single "Remove Ads" non-consumable product. Initializes
    /// against Unity IAP's Fake Store automatically in the Editor/development builds - no real
    /// store credentials needed to test the purchase flow there - and the real App
    /// Store/Play Store in a real device build (once those store listings/products exist,
    /// which is outside what this project setup can automate). Implements IPurchaseState so
    /// AdsGate can check Remove-Ads status without depending on Unity IAP types directly.
    /// </summary>
    public class IAPService : IDetailedStoreListener, IPurchaseState
    {
        public const string RemoveAdsProductId = "remove_ads";

        private IStoreController _storeController;
        private IExtensionProvider _extensions;

        public bool AdsRemoved { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyRemoveAds()
        {
            if (_storeController == null)
            {
                Debug.LogWarning("[IAP] BuyRemoveAds called before the store finished initializing.");
                return;
            }

            _storeController.InitiatePurchase(RemoveAdsProductId);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            IsInitialized = true;
            _storeController = controller;
            _extensions = extensions;

            var product = controller.products.WithID(RemoveAdsProductId);
            AdsRemoved = product != null && product.hasReceipt;
        }

        /// <summary>Restores prior purchases. On iOS this actively re-queries the App Store
        /// (the only platform that needs an explicit restore action); elsewhere purchases are
        /// already restored automatically as part of store initialization, so this just
        /// reports the already-known state.</summary>
        public void RestorePurchases(Action<bool> onComplete)
        {
            var apple = _extensions?.GetExtension<IAppleExtensions>();
            if (apple != null)
            {
                apple.RestoreTransactions((success, message) =>
                {
                    if (!string.IsNullOrEmpty(message)) Debug.Log($"[IAP] RestoreTransactions: {message}");
                    onComplete?.Invoke(success);
                });
                return;
            }

            onComplete?.Invoke(IsInitialized);
        }

        public void OnInitializeFailed(InitializationFailureReason error) =>
            Debug.LogWarning($"[IAP] Initialize failed: {error}");

        public void OnInitializeFailed(InitializationFailureReason error, string message) =>
            Debug.LogWarning($"[IAP] Initialize failed: {error} - {message}");

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            if (purchaseEvent.purchasedProduct.definition.id == RemoveAdsProductId)
            {
                AdsRemoved = true;
                Debug.Log("[IAP] Remove Ads purchased.");
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
            Debug.LogWarning($"[IAP] Purchase failed: {reason}");

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) =>
            Debug.LogWarning($"[IAP] Purchase failed: {description.reason} - {description.message}");
    }
}
