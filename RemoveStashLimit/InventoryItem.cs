using System;
using UnityEngine;

namespace TheForest.Items.Inventory
{
	// Token: 0x02000C9D RID: 3229
	[Serializable]
	public class InventoryItem
	{
		// Token: 0x06005596 RID: 21910
		public int Add(int amount, bool isEquiped)
		{
			this._amount += amount;
			return 0;
		}

		// Token: 0x06005597 RID: 21911
		public int RemoveOverflow(int amount)
		{
			int num = Mathf.Max(amount - this._amount, 0);
			this._amount -= amount - num;
			return num;
		}

		// Token: 0x06005598 RID: 21912
		public bool Remove(int amount)
		{
			if (this._amount - amount >= 0)
			{
				this._amount -= amount;
				return true;
			}
			return false;
		}

		// Token: 0x1700089E RID: 2206
		// (get) Token: 0x06005599 RID: 21913
		public int MaxAmount
		{
			get
			{
				return int.MaxValue;
			}
		}

		// Token: 0x04005BEE RID: 23534
		public int _itemId;

		// Token: 0x04005BEF RID: 23535
		public int _amount;

		// Token: 0x04005BF0 RID: 23536
		public int _maxAmount;

		// Token: 0x04005BF1 RID: 23537
		public int _maxAmountBonus;
	}
}
