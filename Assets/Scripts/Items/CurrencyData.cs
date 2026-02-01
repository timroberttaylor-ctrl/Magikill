using UnityEngine;

namespace Magikill.Items
{
    /// <summary>
    /// ScriptableObject for currency items (gold, coins, etc.).
    /// Defines currency value when picked up.
    /// Create via: Assets > Create > Magikill > Items > Currency
    /// </summary>
    [CreateAssetMenu(fileName = "New Currency", menuName = "Magikill/Items/Currency", order = 4)]
    public class CurrencyData : ItemData
    {
        [Header("Currency Properties")]
        [Tooltip("Amount of gold this currency item represents")]
        public int goldValue = 1;

        protected override void OnValidate()
        {
            base.OnValidate();

            // Ensure itemType is set to Currency
            itemType = ItemType.Currency;

            // Currency should be stackable
            isStackable = true;
            maxStackSize = 999999; // Effectively unlimited

            // Currency items shouldn't have buy/sell prices (they ARE the price)
            buyPrice = 0;
            sellPrice = 0;

            // Ensure gold value is positive
            if (goldValue < 1)
            {
                goldValue = 1;
            }
        }
    }
}
