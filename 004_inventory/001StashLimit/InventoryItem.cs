using System;
using UnityEngine;

namespace TheForest.Items.Inventory
{
	// Token: 0x02000C9D RID: 3229
	[Serializable]
	public class InventoryItem
	{
		// Token: 0x06005596 RID: 21910 RVA: 0x00264EE4 File Offset: 0x002632E4
		public int Add(int amount, bool isEquiped)
		{
			this._amount += amount;
			int num = ((!isEquiped) ? 0 : 1);
			if (this._amount + num > this.MaxAmount)
			{
				int num2 = this._amount + num - this.MaxAmount;
				this._amount -= num2;
				return num2;
			}
			return 0;
		}

		// Token: 0x06005597 RID: 21911 RVA: 0x00264F44 File Offset: 0x00263344
		public int RemoveOverflow(int amount)
		{
			int num = Mathf.Max(amount - this._amount, 0);
			this._amount -= amount - num;
			return num;
		}

		// Token: 0x06005598 RID: 21912 RVA: 0x00264F71 File Offset: 0x00263371
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
		// (get) Token: 0x06005599 RID: 21913 RVA: 0x00264F92 File Offset: 0x00263392
		public int MaxAmount
		{
			get
			{
				return this._maxAmount + this._maxAmountBonus;
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
