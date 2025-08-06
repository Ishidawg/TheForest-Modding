using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FMOD.Studio;
using TheForest.Items.Core;
using TheForest.Items.Craft.Interfaces;
using TheForest.Items.Inventory;
using TheForest.Items.Special;
using TheForest.Items.World;
using TheForest.Player;
using TheForest.Tools;
using TheForest.Utils;
using UniLinq;
using UnityEngine;

namespace TheForest.Items.Craft
{
	// Token: 0x02000C74 RID: 3188
	[DoNotSerializePublic]
	[AddComponentMenu("Items/Craft/Crafting Cog")]
	public class CraftingCog : MonoBehaviour, IItemStorage
	{
		// Token: 0x06005484 RID: 21636 RVA: 0x0028BC24 File Offset: 0x0028A024
		public void Awake()
		{
			if (!this._initialized)
			{
				this._initialized = true;
				this._validRecipeFill = 0f;
				this._validRecipe = null;
				this.ShowCogRenderer(false);
				this._normalMaterial = this._targetCogRenderer.sharedMaterial;
				this._upgradeCog = base.GetComponent<UpgradeCog>();
				this._ingredients = new HashSet<ReceipeIngredient>();
				this.BuildViewCache();
				if (this._inventory && Scene.HudGui)
				{
					this._clickToCombineButton = Scene.HudGui.ClickToCombineInfo;
				}
				if (this._craftSfx)
				{
					this._craftSfxEmitter = this._craftSfx.GetComponent<FMOD_StudioEventEmitter>();
				}
				if (this._craftSfx2)
				{
					this._craftSfx2Emitter = this._craftSfx2.GetComponent<FMOD_StudioEventEmitter>();
				}
				for (int i = 0; i < this._itemViews.Length; i++)
				{
					this._itemViews[i].Init();
				}
				GameObject gameObject = new GameObject("LambdaMultiview");
				gameObject.SetActive(false);
				this._lambdaMultiView = gameObject.AddComponent<InventoryItemView>();
				this._lambdaMultiView.transform.parent = base.transform;
				this._lambdaMultiView.transform.localPosition = new Vector3(-2.5f, 2.5f, 2.5f);
				this._lambdaMultiView._isCraft = true;
				this._lambdaMultiView._allowMultiView = true;
				this._lambdaMultiView._itemId = -1;
				gameObject.SetActive(true);
			}
		}

		// Token: 0x06005485 RID: 21637 RVA: 0x0028BDA4 File Offset: 0x0028A1A4
		private void Start()
		{
			if (!LevelSerializer.IsDeserializing)
			{
				this.IngredientCleanUp();
			}
			if (FMOD_StudioSystem.instance && !CoopPeerStarter.DedicatedHost)
			{
				this.CogRotateEventInstance = FMOD_StudioSystem.instance.GetEvent("event:/ui/ingame/ui_cog_spin");
				if (this.CogRotateEventInstance != null)
				{
					UnityUtil.ERRCHECK(this.CogRotateEventInstance.getCue("KeyOff", out this.CogRotateEventKeyoff));
				}
			}
		}

		// Token: 0x06005486 RID: 21638 RVA: 0x0028BE1C File Offset: 0x0028A21C
		public void OnDeserialized()
		{
			this.Awake();
		}

		// Token: 0x06005487 RID: 21639 RVA: 0x0028BE24 File Offset: 0x0028A224
		private void OnEnable()
		{
			foreach (KeyValuePair<int, InventoryItemView> keyValuePair in this._itemViewsCache)
			{
				SpecialItemControlerBase component = keyValuePair.Value.GetComponent<SpecialItemControlerBase>();
				if (!(component == null))
				{
					MetalTinTrayControler metalTinTrayControler = component as MetalTinTrayControler;
					if (!(metalTinTrayControler == null))
					{
						if (!(metalTinTrayControler._storage == null) && !metalTinTrayControler._storage.IsEmpty)
						{
							metalTinTrayControler.EmptyToInventory();
						}
					}
				}
			}
			InventoryItemView rightHand = this._inventory.RightHand;
			ItemStorageProxy itemStorageProxy = ((!(rightHand == null)) ? rightHand.GetComponent<ItemStorageProxy>() : null);
			if (itemStorageProxy != null && itemStorageProxy._storage != null)
			{
				int itemId = rightHand._itemId;
				ItemProperties properties = rightHand.Properties;
				this._inventory.RemoveItem(itemId, 1, true, true);
				this._inventory.AddItem(itemId, 1, true, true, properties);
			}
		}

		// Token: 0x06005488 RID: 21640 RVA: 0x0028BF5C File Offset: 0x0028A35C
		private void OnDisable()
		{
			if (this._clickToCombineButton.activeSelf)
			{
				this._clickToCombineButton.SetActive(false);
				this._targetCogRenderer.sharedMaterial = this._normalMaterial;
			}
			if (Scene.HudGui && LocalPlayer.Inventory.CurrentView != PlayerInventory.PlayerViews.Inventory)
			{
				Scene.HudGui.ShowValidCraftingRecipes(null);
				Scene.HudGui.HideUpgradesDistribution();
			}
		}

		// Token: 0x06005489 RID: 21641 RVA: 0x0028BFCC File Offset: 0x0028A3CC
		private void OnMouseExitCollider()
		{
			if (this.CogRotateEventKeyoff != null && this.CogRotateEventKeyoff.isValid())
			{
				UnityUtil.ERRCHECK(this.CogRotateEventKeyoff.trigger());
			}
			this._hovered = false;
			this.OnDisable();
		}

		// Token: 0x0600548A RID: 21642 RVA: 0x0028C018 File Offset: 0x0028A418
		private void OnMouseEnterCollider()
		{
			bool flag = this.CraftOverride != null || this.CanStore;
			bool hasValideRecipe = this.HasValideRecipe;
			if (!this._hovered && !this._upgradeCog.enabled && ((hasValideRecipe && (this._validRecipe._type != Receipe.Types.Upgrade || this._upgradeCount > 0)) || flag))
			{
				this._hovered = true;
				this._targetCogRenderer.sharedMaterial = this._selectedMaterial;
				if (this._clickToCombineButton.activeSelf != (hasValideRecipe || flag))
				{
					this._clickToCombineButton.SetActive(hasValideRecipe || flag);
				}
			}
			UnityUtil.ERRCHECK(this.CogRotateEventInstance.set3DAttributes(UnityUtil.to3DAttributes(LocalPlayer.MainCamTr.gameObject, null)));
			UnityUtil.ERRCHECK(this.CogRotateEventInstance.start());
		}

		// Token: 0x0600548B RID: 21643 RVA: 0x0028C100 File Offset: 0x0028A500
		private void Update()
		{
			if (!this._upgradeCog.enabled && (this.CraftOverride == null || this.CraftOverride.CanCombine()))
			{
				if ((this._hovered && (TheForest.Utils.Input.GetButtonDown("Combine") || TheForest.Utils.Input.GetButtonDown("Build"))) || TheForest.Utils.Input.GetButtonDown("Jump"))
				{
					if (this.CraftOverride != null)
					{
						this.CraftOverride.Combine();
					}
					else if (this.CanStore)
					{
						this.DoStorage();
					}
					else if (this.CanCraft)
					{
						this.DoCraft();
					}
				}
				else if (TheForest.Utils.Input.GetButtonDown("Drop"))
				{
					if (this._ingredients != null && this._ingredients.Count > 0)
					{
						LocalPlayer.Sfx.PlayWhoosh();
					}
					this.Close();
				}
				bool flag = this._ingredients.Count > 0 && !this._upgradeCog.enabled;
				if (Scene.HudGui.DropToRemoveAllInfo.activeSelf != flag)
				{
					Scene.HudGui.DropToRemoveAllInfo.SetActive(flag);
				}
			}
			this.UpdateCogRotation();
		}

		// Token: 0x0600548C RID: 21644 RVA: 0x0028C240 File Offset: 0x0028A640
		private void UpdateCogRotation()
		{
			float num = 0f;
			if (this._hovered)
			{
				num = 2f;
			}
			if (this._targetCogRenderer.enabled)
			{
				this._targetCogRotate = Mathf.Lerp(this._targetCogRotate, num, Time.unscaledDeltaTime * 2.5f);
			}
			else
			{
				this._targetCogRotate = 0f;
			}
			this._targetCogRenderer.transform.localEulerAngles += new Vector3(0f, this._targetCogRotate, 0f);
		}

		// Token: 0x0600548D RID: 21645 RVA: 0x0028C2D1 File Offset: 0x0028A6D1
		public bool IsValidItem(Item item)
		{
			if (this.Storage != null)
			{
				return this.Storage.IsValidItem(item);
			}
			return this.MatchType(this._acceptedTypes, item._type);
		}

		// Token: 0x0600548E RID: 21646 RVA: 0x0028C303 File Offset: 0x0028A703
		private bool MatchType(Item.Types mask, Item.Types type)
		{
			return (mask & type) != (Item.Types)0;
		}

		// Token: 0x17000883 RID: 2179
		// (get) Token: 0x0600548F RID: 21647 RVA: 0x0028C30E File Offset: 0x0028A70E
		public bool IsEmpty
		{
			get
			{
				return this._ingredients.Count == 0;
			}
		}

		// Token: 0x06005490 RID: 21648 RVA: 0x0028C320 File Offset: 0x0028A720
		public int Add(int itemId, int amount = 1, ItemProperties properties = null)
		{
			if (amount > 0)
			{
				if (this.Storage != null)
				{
					this.RemoveExcessStorage(itemId);
				}
				ReceipeIngredient receipeIngredient = this.TryGetIngredient(itemId);
				int num;
				if (this.Storage || !this.InItemViewsCache(itemId))
				{
					num = int.MaxValue;
				}
				else if (this._itemViewsCache[itemId]._allowMultiView)
				{
					num = this._itemViewsCache[itemId]._maxMultiViews;
				}
				else
				{
					num = 1;
				}
				if (receipeIngredient == null)
				{
					receipeIngredient = new ReceipeIngredient
					{
						_itemID = itemId
					};
					this._ingredients.Add(receipeIngredient);
				}
				int num2 = Mathf.Max(receipeIngredient._amount + amount - num, 0);
				receipeIngredient._amount = Mathf.Min(receipeIngredient._amount + amount, num);
				this.ToggleItemInventoryView(itemId, properties);
				this.CheckForValidRecipe();
				return num2;
			}
			return 0;
		}

		// Token: 0x06005491 RID: 21649 RVA: 0x0028C404 File Offset: 0x0028A804
		private void RemoveExcessStorage(int incomingItemId)
		{
			List<ReceipeIngredient> list = new List<ReceipeIngredient>();
			foreach (ReceipeIngredient receipeIngredient in this._ingredients)
			{
				int itemID = receipeIngredient._itemID;
				Item item = ItemDatabase.ItemById(itemID);
				if (item._maxAmount > 0 && (item._maxAmount <= 100 || itemID != incomingItemId))
				{
					bool flag = this.InItemViewsCache(itemID);
					ItemStorageProxy itemStorageProxy = ((!flag) ? null : this._itemViewsCache[itemID].GetComponent<ItemStorageProxy>());
					if (!(itemStorageProxy != null) || !(itemStorageProxy._storage == this.Storage))
					{
						list.Add(receipeIngredient);
					}
				}
			}
			for (int i = 0; i < list.Count - (this.Storage._slotCount - 1); i++)
			{
				ItemProperties itemProperties = ItemProperties.Any;
				if (this.InItemViewsCache(list[i]._itemID))
				{
					itemProperties = this._itemViewsCache[list[i]._itemID].Properties;
				}
				this._inventory.AddItem(list[i]._itemID, list[i]._amount, true, true, itemProperties);
				this.Remove(list[i]._itemID, list[i]._amount, itemProperties);
			}
		}

		// Token: 0x06005492 RID: 21650 RVA: 0x0028C5A4 File Offset: 0x0028A9A4
		public int Remove(int itemId, int amount = 1, ItemProperties properties = null)
		{
			if (this._upgradeCog.enabled)
			{
				this._upgradeCog.Shutdown();
			}
			ReceipeIngredient receipeIngredient = this.TryGetIngredient(itemId);
			if (receipeIngredient != null)
			{
				bool flag = this.InItemViewsCache(itemId);
				InventoryItemView inventoryItemView = ((!flag) ? null : this._itemViewsCache[itemId]);
				if (flag && (inventoryItemView.ItemCache._maxAmount == 0 || inventoryItemView.ItemCache._maxAmount > LocalPlayer.Inventory.InventoryItemViewsCache[itemId].Count || !inventoryItemView._allowMultiView))
				{
					if (properties == ItemProperties.Any || inventoryItemView.Properties.Match(properties))
					{
						int num = Mathf.Max(amount - receipeIngredient._amount, 0);
						if ((receipeIngredient._amount -= amount) <= 0)
						{
							this._ingredients.Remove(receipeIngredient);
						}
						this.CheckForValidRecipe();
						this.ToggleItemInventoryView(itemId, properties);
						return num;
					}
				}
				else
				{
					if (flag)
					{
						int num2 = inventoryItemView.AmountOfMultiviewWithProperties(itemId, properties);
						int num3 = Mathf.Max(amount - num2, 0);
						if ((receipeIngredient._amount -= amount - num3) <= 0)
						{
							this._ingredients.Remove(receipeIngredient);
						}
						this.CheckForValidRecipe();
						inventoryItemView.RemovedMultiViews(itemId, amount, properties, false);
						this.SelectItemViewProxyTarget();
						return num3;
					}
					if (LocalPlayer.Inventory.InventoryItemViewsCache.ContainsKey(itemId))
					{
						int num4 = this._lambdaMultiView.AmountOfMultiviewWithProperties(itemId, properties);
						int num5 = Mathf.Max(amount - num4, 0);
						if ((receipeIngredient._amount -= amount - num5) <= 0)
						{
							this._ingredients.Remove(receipeIngredient);
						}
						this.CheckForValidRecipe();
						this._lambdaMultiView.RemovedMultiViews(itemId, amount - num5, properties, false);
						this.SelectItemViewProxyTarget();
						return num5;
					}
					this._completedItemViewProxy.Unset();
				}
			}
			return amount;
		}

		// Token: 0x06005493 RID: 21651 RVA: 0x0028C78C File Offset: 0x0028AB8C
		public ItemProperties GetPropertiesOf(int itemId)
		{
			ReceipeIngredient receipeIngredient = this.TryGetIngredient(itemId);
			if (receipeIngredient != null)
			{
				InventoryItemView inventoryItemView = null;
				if (this._itemViewsCache.TryGetValue(itemId, out inventoryItemView) && (inventoryItemView.ItemCache._maxAmount == 0 || inventoryItemView.ItemCache._maxAmount > LocalPlayer.Inventory.InventoryItemViewsCache[itemId].Count || !inventoryItemView._allowMultiView))
				{
					return inventoryItemView.Properties;
				}
				if (inventoryItemView != null)
				{
					return inventoryItemView.GetFirstViewProperties();
				}
				if (LocalPlayer.Inventory.InventoryItemViewsCache.ContainsKey(itemId))
				{
					return this._lambdaMultiView.GetFirstViewPropertiesForItem(itemId);
				}
			}
			return null;
		}

		// Token: 0x06005494 RID: 21652 RVA: 0x0028C839 File Offset: 0x0028AC39
		public void Open()
		{
		}

		// Token: 0x06005495 RID: 21653 RVA: 0x0028C83C File Offset: 0x0028AC3C
		public void Close()
		{
			if (this._upgradeCog.enabled)
			{
				this._upgradeCog.Shutdown();
			}
			if (this.Storage != null)
			{
				this.EmptyStorageToInventory();
			}
			foreach (ReceipeIngredient receipeIngredient in this._ingredients)
			{
				if (LocalPlayer.Inventory.InventoryItemViewsCache[receipeIngredient._itemID][0].ItemCache.MatchType(Item.Types.Special))
				{
					LocalPlayer.Inventory.SpecialItemsControlers[receipeIngredient._itemID].ToggleSpecialCraft(false);
				}
				if (this.InItemViewsCache(receipeIngredient._itemID))
				{
					InventoryItemView inventoryItemView = this._itemViewsCache[receipeIngredient._itemID];
					if (!inventoryItemView._allowMultiView)
					{
						this._inventory.AddItem(receipeIngredient._itemID, receipeIngredient._amount, true, true, inventoryItemView.Properties);
					}
					else
					{
						int i = receipeIngredient._amount;
						while (i > 0)
						{
							ItemProperties firstViewProperties = inventoryItemView.GetFirstViewProperties();
							int num = inventoryItemView.AmountOfMultiviewWithProperties(receipeIngredient._itemID, firstViewProperties);
							inventoryItemView.RemovedMultiViews(receipeIngredient._itemID, num, firstViewProperties, false);
							this._inventory.AddItem(receipeIngredient._itemID, num, true, true, firstViewProperties);
							if (num > 0)
							{
								i -= num;
							}
							else if (firstViewProperties == ItemProperties.Any)
							{
								break;
							}
						}
						if (i > 0)
						{
							this._inventory.AddItem(receipeIngredient._itemID, receipeIngredient._amount, true, true, ItemProperties.Any);
						}
					}
				}
				else if (LocalPlayer.Inventory.InventoryItemViewsCache.ContainsKey(receipeIngredient._itemID))
				{
					this._lambdaMultiView.SetAnyMultiViewAmount(LocalPlayer.Inventory.InventoryItemViewsCache[receipeIngredient._itemID][0], this._lambdaMultiView.transform, 0, ItemProperties.Any, true);
				}
				else
				{
					this._inventory.AddItem(receipeIngredient._itemID, receipeIngredient._amount, true, true, ItemProperties.Any);
				}
			}
			this.IngredientCleanUp();
			this.CheckForValidRecipe();
			Scene.HudGui.ShowValidCraftingRecipes(null);
			Scene.HudGui.HideUpgradesDistribution();
			Scene.HudGui.DropToRemoveAllInfo.SetActive(false);
		}

		// Token: 0x06005496 RID: 21654 RVA: 0x0028CAB8 File Offset: 0x0028AEB8
		private void EmptyStorageToInventory()
		{
			for (int i = 0; i < this.Storage.UsedSlots.Count; i++)
			{
				LocalPlayer.Inventory.AddItem(this.Storage.UsedSlots[i]._itemId, this.Storage.UsedSlots[i]._amount, true, true, this.Storage.UsedSlots[i]._properties);
			}
			this.Storage.Close();
			this.Storage.UsedSlots.Clear();
			this.Storage.UpdateContentVersion();
		}

		// Token: 0x06005497 RID: 21655 RVA: 0x0028CB5C File Offset: 0x0028AF5C
		private void BuildViewCache()
		{
			this._itemViewsCache = this._itemViews.Where((InventoryItemView iv) => !ItemDatabase.ItemById(iv._itemId).MatchType(Item.Types.Extension)).ToDictionary((InventoryItemView iv) => iv._itemId, (InventoryItemView iv) => iv);
			this._itemExtensionViewsCache = this._itemViews.Where((InventoryItemView iv) => ItemDatabase.ItemById(iv._itemId).MatchType(Item.Types.Extension)).ToDictionary((InventoryItemView iv) => iv._itemId, (InventoryItemView iv) => iv);
		}

		// Token: 0x06005498 RID: 21656 RVA: 0x0028CC44 File Offset: 0x0028B044
		private bool CanCarryProduct(Receipe recipe)
		{
			int maxAmountOf = LocalPlayer.Inventory.GetMaxAmountOf(recipe._productItemID);
			return this._inventory.AmountOf(recipe._productItemID, false) < maxAmountOf;
		}

		// Token: 0x06005499 RID: 21657 RVA: 0x0028CC79 File Offset: 0x0028B079
		private bool ShouldList(Receipe recipe)
		{
			return !recipe._hidden;
		}

		// Token: 0x0600549A RID: 21658 RVA: 0x0028CC84 File Offset: 0x0028B084
		private bool CanCarryUpgradeProduct(Receipe recipe)
		{
			if (recipe._forceUnique && this.HasExistingUpgradeBonus(recipe))
			{
				return false;
			}
			if (recipe._productItemID == recipe._ingredients[0]._itemID)
			{
				return true;
			}
			int maxAmountOf = LocalPlayer.Inventory.GetMaxAmountOf(recipe._productItemID);
			int num = this._inventory.AmountOf(recipe._productItemID, false);
			return num < maxAmountOf || this._ingredients.Any((ReceipeIngredient i) => i._itemID == recipe._productItemID);
		}

		// Token: 0x0600549B RID: 21659 RVA: 0x0028CD3C File Offset: 0x0028B13C
		private bool HasExistingUpgradeBonus(Receipe recipe)
		{
			if (recipe._type != Receipe.Types.Upgrade)
			{
				return false;
			}
			if (this._inventory.AmountOf(recipe._productItemID, false) == 0 && this.Ingredients.All((ReceipeIngredient ingredient) => ingredient._itemID != recipe._productItemID))
			{
				return false;
			}
			if (recipe._weaponStatUpgrades == null || recipe._weaponStatUpgrades.Length == 0)
			{
				return false;
			}
			int productItemID = recipe._productItemID;
			WeaponStatUpgrade.Types activeBonus = this._itemViewsCache[productItemID].ActiveBonus;
			if (activeBonus == (WeaponStatUpgrade.Types)(-1))
			{
				return false;
			}
			foreach (WeaponStatUpgrade weaponStatUpgrade in recipe._weaponStatUpgrades)
			{
				if (activeBonus == weaponStatUpgrade._type)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600549C RID: 21660 RVA: 0x0028CE30 File Offset: 0x0028B230
		private bool CheckStorage()
		{
			if (this.Storage)
			{
				this._validRecipe = null;
				Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
				Scene.HudGui.ShowValidCraftingRecipes(null);
				Scene.HudGui.HideUpgradesDistribution();
				this.ShowCogRenderer(this._ingredients.Count > 1);
				return true;
			}
			if (this._validRecipe == null)
			{
				this.ShowCogRenderer(false);
			}
			return false;
		}

		// Token: 0x0600549D RID: 21661 RVA: 0x0028CEA7 File Offset: 0x0028B2A7
		private void ShowCogRenderer(bool enabledValue)
		{
			this._targetCogRenderer.enabled = enabledValue;
			base.gameObject.GetComponent<Collider>().enabled = enabledValue;
		}

		// Token: 0x0600549E RID: 21662 RVA: 0x0028CEC8 File Offset: 0x0028B2C8
		private void DoStorage()
		{
			List<ReceipeIngredient> list = new List<ReceipeIngredient>();
			InventoryItemView inventoryItemView = null;
			bool flag = false;
			this._craftSfxEmitter.Play();
			foreach (ReceipeIngredient receipeIngredient in this._ingredients)
			{
				bool flag2 = this.InItemViewsCache(receipeIngredient._itemID);
				ItemStorageProxy itemStorageProxy = ((!flag2) ? null : this._itemViewsCache[receipeIngredient._itemID].GetComponent<ItemStorageProxy>());
				if (!flag2)
				{
					while (this._lambdaMultiView.ContainsMultiView(receipeIngredient._itemID))
					{
						ItemProperties firstViewPropertiesForItem = this._lambdaMultiView.GetFirstViewPropertiesForItem(receipeIngredient._itemID);
						int num = this._lambdaMultiView.AmountOfMultiviewWithProperties(receipeIngredient._itemID, firstViewPropertiesForItem);
						int num2 = this.Storage.Add(receipeIngredient._itemID, num, firstViewPropertiesForItem);
						this._lambdaMultiView.RemovedMultiViews(receipeIngredient._itemID, num - num2, firstViewPropertiesForItem, false);
						if (num2 == 0)
						{
							list.Add(receipeIngredient);
						}
						else
						{
							if (num2 == num)
							{
								break;
							}
							receipeIngredient._amount = num2;
						}
					}
				}
				else if (itemStorageProxy == null)
				{
					int num3 = 0;
					if (!this._itemViewsCache[receipeIngredient._itemID]._allowMultiView)
					{
						num3 = this.Storage.Add(receipeIngredient._itemID, receipeIngredient._amount, this._itemViewsCache[receipeIngredient._itemID].Properties);
					}
					else
					{
						int i = receipeIngredient._amount;
						while (i > 0)
						{
							ItemProperties firstViewProperties = this._itemViewsCache[receipeIngredient._itemID].GetFirstViewProperties();
							int num4 = this._itemViewsCache[receipeIngredient._itemID].AmountOfMultiviewWithProperties(receipeIngredient._itemID, firstViewProperties);
							int num5 = this.Storage.Add(receipeIngredient._itemID, num4, firstViewProperties);
							if (num4 == num5)
							{
								num3 = i;
								break;
							}
							num4 -= num5;
							this._itemViewsCache[receipeIngredient._itemID].RemovedMultiViews(receipeIngredient._itemID, num4, firstViewProperties, false);
							if (num4 > 0)
							{
								i -= num4;
							}
							else if (firstViewProperties == ItemProperties.Any)
							{
								num3 = i;
								break;
							}
						}
					}
					if (num3 == 0)
					{
						list.Add(receipeIngredient);
					}
					else
					{
						receipeIngredient._amount = num3;
					}
				}
				else if (itemStorageProxy._storage == this.Storage)
				{
					inventoryItemView = this._itemViewsCache[receipeIngredient._itemID];
					flag = true;
				}
			}
			foreach (ReceipeIngredient receipeIngredient2 in list)
			{
				this._ingredients.Remove(receipeIngredient2);
				this.ToggleItemInventoryView(receipeIngredient2._itemID, ItemProperties.Any);
			}
			this.Storage.UpdateContentVersion();
			this.CheckStorage();
			this._completedItemViewProxy._targetView = inventoryItemView;
			if (flag)
			{
				int itemId = inventoryItemView._itemId;
				this._inventory.AddItem(itemId, 1, true, true, inventoryItemView.Properties);
				this._inventory.Equip(itemId, false);
				this._inventory.CurrentStorage.Remove(itemId, 1, null);
				this._inventory.Close();
			}
		}

		// Token: 0x0600549F RID: 21663 RVA: 0x0028D270 File Offset: 0x0028B670
		private bool InItemViewsCache(int itemId)
		{
			return this._itemViewsCache.ContainsKey(itemId);
		}

		// Token: 0x060054A0 RID: 21664 RVA: 0x0028D280 File Offset: 0x0028B680
		private void DoCraft()
		{
			Receipe validRecipe = this._validRecipe;
			this._craftSfxEmitter.Play();
			global::UnityEngine.Object.Instantiate<GameObject>(this._craftParticle1, this._craftParticleSpawnPos.position, this._craftParticleSpawnPos.rotation);
			int num = (validRecipe._type.Equals(Receipe.Types.Upgrade) ? this._upgradeCount : 1);
			ItemProperties itemProperties = ItemProperties.Any;
			for (int i = 0; i < validRecipe._ingredients.Length; i++)
			{
				ReceipeIngredient receipeIngredient = validRecipe._ingredients[i];
				ReceipeIngredient receipeIngredient2 = this.TryGetIngredient(receipeIngredient._itemID);
				int num2;
				if (i == 0)
				{
					if (validRecipe._type.Equals(Receipe.Types.Upgrade))
					{
						num2 = receipeIngredient._amount;
					}
					else if (validRecipe._type.Equals(Receipe.Types.Extension))
					{
						num2 = 0;
					}
					else
					{
						num2 = receipeIngredient._amount * num;
					}
				}
				else
				{
					num2 = receipeIngredient._amount * num;
				}
				receipeIngredient2._amount -= num2;
				if (i == 1)
				{
					itemProperties = ((!this._itemViewsCache[receipeIngredient2._itemID]._allowMultiView) ? this._itemViewsCache[receipeIngredient2._itemID].Properties : this._itemViewsCache[receipeIngredient2._itemID].GetFirstViewProperties());
				}
				if (receipeIngredient2._amount <= 0)
				{
					this._ingredients.Remove(receipeIngredient2);
				}
				else if (receipeIngredient._amount == 0)
				{
					this._inventory.AddItem(receipeIngredient._itemID, receipeIngredient2._amount, true, true, itemProperties);
					this.Remove(receipeIngredient._itemID, receipeIngredient2._amount, null);
				}
				this.ToggleItemInventoryView(receipeIngredient._itemID, ItemProperties.Any);
			}
			int num3 = num;
			if (!validRecipe._type.Equals(Receipe.Types.Extension))
			{
				this.Add(validRecipe._productItemID, validRecipe._productItemAmount, ItemProperties.Any);
			}
			else
			{
				LocalPlayer.Inventory.AddItem(validRecipe._productItemID, validRecipe._productItemAmount, true, true, null);
				this.CheckForValidRecipe();
			}
			if (validRecipe._type.Equals(Receipe.Types.Upgrade))
			{
				this.ApplyUpgrade(validRecipe, itemProperties, num3);
			}
			if (validRecipe._ingredients[0]._itemID != validRecipe._productItemID)
			{
				EventRegistry.Player.Publish(TfEvent.CraftedItem, validRecipe._productItemID);
			}
			InventoryItemView itemView = this.GetItemView(validRecipe._productItemID);
			this._completedItemViewProxy._targetView = itemView;
			if (!this._upgradeCog.enabled && itemView)
			{
				base.StartCoroutine(this.animateCraftedItemRoutine(itemView.transform));
			}
		}

		// Token: 0x060054A1 RID: 21665 RVA: 0x0028D574 File Offset: 0x0028B974
		private void CheckCraftOverride()
		{
			Scene.HudGui.HideUpgradesDistribution();
			bool flag = this.CraftOverride.CanCombine();
			if (flag)
			{
				this._craftSfx2Emitter.Play();
			}
			this.ShowCogRenderer(flag);
		}

		// Token: 0x060054A2 RID: 21666 RVA: 0x0028D5B0 File Offset: 0x0028B9B0
		public void CheckForValidRecipe()
		{
			if (this._legacyCraftingSystem)
			{
				this.CheckForValidRecipeLegacy();
				return;
			}
			if (this.CheckStorage())
			{
				return;
			}
			this._validRecipeFill = 0f;
			this._validRecipe = null;
			this._upgradeCount = 0;
			this._validRecipeFill = 0f;
			this._validRecipeFull = false;
			if (this.CraftOverride != null)
			{
				this.HideRecipeDisplay();
				this.CheckCraftOverride();
				return;
			}
			if (this._ingredients.Count == 0)
			{
				this.HideRecipeDisplay();
				return;
			}
			int i;
			List<Receipe> list = this._receipeBook.AvailableReceipesCache.Where((Receipe ar) => this._ingredients.All((ReceipeIngredient i) => ar._ingredients.Any((ReceipeIngredient i2) => i._itemID == i2._itemID))).ToList<Receipe>();
			list.AddRange(this._receipeBook.AvailableUpgradeCache.Where((Receipe ar) => this._ingredients.All((ReceipeIngredient i) => ar._ingredients.Any((ReceipeIngredient i2) => i._itemID == i2._itemID))));
			if (list.Count == 0)
			{
				this.HideRecipeDisplay();
				return;
			}
			Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(true);
			list.ForEach(delegate(Receipe ar)
			{
				ar.CanCarryProduct = this.CanCarryUpgradeProduct(ar);
			});
			bool flag = false;
			bool flag2 = false;
			int num = 0;
			list = (from eachRecipe in list
				orderby eachRecipe.CanCarryProduct descending, eachRecipe._ingredients.Length
				select eachRecipe).ThenBy((Receipe r) => r._type).ToList<Receipe>();
			for (i = 0; i < list.Count; i++)
			{
				Receipe receipe = list[i];
				if (receipe.CanCarryProduct)
				{
					flag2 |= (receipe._type == Receipe.Types.Craft) | (receipe._type == Receipe.Types.Extension);
					int num2 = receipe._ingredients.Sum((ReceipeIngredient ingredient) => Mathf.Max(ingredient._amount, 1));
					int matchedIngredientSum = CraftingCog.GetMatchedIngredientSum(receipe, this._ingredients);
					float num3 = (float)matchedIngredientSum / (float)num2;
					bool flag3 = num2 == matchedIngredientSum;
					if (receipe._hidden)
					{
						num3 = 0f;
					}
					if ((!flag && num3 > this._validRecipeFill) || (flag && flag3 && num < num2))
					{
						this._validRecipe = receipe;
						this._validRecipeFill = Mathf.Max(num3, this._validRecipeFill);
						num = num2;
						flag = flag3;
					}
				}
			}
			if (flag && flag2)
			{
				if (this._validRecipe._type == Receipe.Types.Upgrade)
				{
					this._upgradeCount = ItemDatabase.ItemById(this._validRecipe._productItemID)._maxUpgradesAmount;
				}
				Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
				Scene.HudGui.HideUpgradesDistribution();
				this._craftSfx2Emitter.Play();
				this.ShowCogRenderer(true);
				return;
			}
			CraftingCog.ShowFilteredRecipes(list);
			this.ShowCogRenderer(false);
			float validRecipeFill = this._validRecipeFill;
			this._validRecipe = null;
			this.CheckForValidUpgrade(false);
			if (this._validRecipe == null)
			{
				this._validRecipeFill = validRecipeFill;
				Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(true);
				this.ShowCogRenderer(false);
			}
			Scene.HudGui.CraftingReceipeProgress.fillAmount = this._validRecipeFill;
		}

		// Token: 0x060054A3 RID: 21667 RVA: 0x0028D8EB File Offset: 0x0028BCEB
		private void HideRecipeDisplay()
		{
			this.ShowCogRenderer(false);
			Scene.HudGui.ShowValidCraftingRecipes(null);
			Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
		}

		// Token: 0x060054A4 RID: 21668 RVA: 0x0028D914 File Offset: 0x0028BD14
		private static void ShowFilteredRecipes(List<Receipe> foundRecipes)
		{
			IEnumerable<Receipe> enumerable = from eachRecipe in foundRecipes.Distinct(new CraftingCog.CompareRecipeProduct())
				where !eachRecipe._hidden
				select eachRecipe;
			Scene.HudGui.ShowValidCraftingRecipes(enumerable);
		}

		// Token: 0x060054A5 RID: 21669 RVA: 0x0028D95C File Offset: 0x0028BD5C
		[Obsolete("Use CheckForValidRecipe")]
		public void CheckForValidRecipeLegacy()
		{
			if (this.CheckStorage())
			{
				return;
			}
			Receipe receipe = null;
			IOrderedEnumerable<Receipe> orderedEnumerable = null;
			IOrderedEnumerable<Receipe> orderedEnumerable2 = null;
			int num = 0;
			int num2 = 0;
			int i;
			if (this._ingredients.Count > 0 && this.CraftOverride == null)
			{
				orderedEnumerable = from ar in this._receipeBook.AvailableReceipesCache
					where !ar._hidden
					where this._ingredients.All((ReceipeIngredient i) => ar._ingredients.Any((ReceipeIngredient i2) => i._itemID == i2._itemID))
					orderby ar._ingredients.Sum((ReceipeIngredient ari) => ari._amount), ar._ingredients.Length
					select ar;
				num = orderedEnumerable.Count<Receipe>();
				orderedEnumerable.ForEach(delegate(Receipe ar)
				{
					ar.CanCarryProduct = this.CanCarryUpgradeProduct(ar);
				});
				orderedEnumerable2 = from ar in this._receipeBook.AvailableUpgradeCache
					where !ar._hidden
					where this._ingredients.All((ReceipeIngredient i) => ar._ingredients.Any((ReceipeIngredient i2) => i._itemID == i2._itemID))
					orderby ar._ingredients.Length
					select ar;
				num2 = orderedEnumerable2.Count<Receipe>();
				orderedEnumerable2.ForEach(delegate(Receipe ar)
				{
					ar.CanCarryProduct = this.CanCarryUpgradeProduct(ar);
				});
				receipe = orderedEnumerable.FirstOrDefault((Receipe ar) => ar.CanCarryProduct);
				Receipe receipe2 = orderedEnumerable2.FirstOrDefault((Receipe ar) => ar.CanCarryProduct);
				if (receipe2 != null && receipe != null && receipe._ingredients.Length > receipe2._ingredients.Length)
				{
					receipe = null;
				}
			}
			bool flag = receipe != null;
			bool flag2 = flag;
			this._validRecipeFill = 0f;
			if (flag)
			{
				this._validRecipe = null;
				flag = false;
				Receipe receipe3 = null;
				float num3 = 0f;
				List<Receipe> list = new List<Receipe>();
				foreach (Receipe receipe4 in orderedEnumerable)
				{
					int num4 = 0;
					int num5 = receipe4._ingredients.Sum((ReceipeIngredient i) => i._amount);
					using (HashSet<ReceipeIngredient>.Enumerator enumerator2 = this._ingredients.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							ReceipeIngredient cogIngredients = enumerator2.Current;
							ReceipeIngredient receipeIngredient = receipe4._ingredients.First((ReceipeIngredient i) => i._itemID == cogIngredients._itemID);
							num4 += ((cogIngredients._amount <= receipeIngredient._amount) ? cogIngredients._amount : receipeIngredient._amount);
							if (receipeIngredient._amount == 0 && cogIngredients._amount > 0)
							{
								num4++;
								num5++;
							}
						}
					}
					this._validRecipeFull = false;
					this._validRecipeFill = (float)num4 / (float)num5;
					if (num4 != num5)
					{
						if (this._validRecipeFill > num3)
						{
							num3 = this._validRecipeFill;
							receipe3 = receipe4;
							list.Insert(0, receipe4);
						}
						else
						{
							bool flag3 = false;
							for (i = list.Count - 1; i >= 0; i--)
							{
								if (list[i]._ingredients.Length < receipe4._ingredients.Length)
								{
									list.Insert(i + 1, receipe4);
									flag3 = true;
									break;
								}
							}
							if (!flag3)
							{
								list.Add(receipe4);
							}
						}
					}
					else
					{
						list.Insert(0, receipe4);
						this._validRecipe = receipe4;
						flag = true;
						receipe3 = null;
						num3 = 1f;
					}
				}
				flag2 = flag;
				this._validRecipeFill = num3;
				if (receipe3 != null && !flag)
				{
					Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(true);
					Scene.HudGui.CraftingReceipeProgress.fillAmount = num3;
				}
				else
				{
					Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
				}
				Scene.HudGui.ShowValidCraftingRecipes(from r in list.Concat(orderedEnumerable2).Distinct(new CraftingCog.CompareRecipeProduct())
					orderby r.CanCarryProduct descending, r._type
					select r);
			}
			else
			{
				List<Receipe> list2 = new List<Receipe>();
				if (orderedEnumerable != null && num > 0)
				{
					list2.AddRange(orderedEnumerable);
				}
				if (orderedEnumerable2 != null && num2 > 0)
				{
					list2.AddRange(orderedEnumerable2);
				}
				Scene.HudGui.ShowValidCraftingRecipes(from r in list2.Distinct(new CraftingCog.CompareRecipeProduct())
					orderby r.CanCarryProduct descending, r._ingredients.Length
					select r);
				Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
			}
			if (this.CraftOverride == null)
			{
				if (flag)
				{
					Scene.HudGui.HideUpgradesDistribution();
					this._craftSfx2Emitter.Play();
					this._targetCogRenderer.enabled = true;
					base.gameObject.GetComponent<Collider>().enabled = true;
				}
				if (!flag || !this.CanCraft)
				{
					this.CheckForValidUpgrade(flag2);
				}
			}
			else
			{
				Scene.HudGui.HideUpgradesDistribution();
				if (this.CraftOverride.CanCombine())
				{
					this._craftSfx2Emitter.Play();
					this._targetCogRenderer.enabled = true;
					base.gameObject.GetComponent<Collider>().enabled = true;
				}
				else
				{
					this._targetCogRenderer.enabled = false;
					base.gameObject.GetComponent<Collider>().enabled = false;
				}
			}
		}

		// Token: 0x060054A6 RID: 21670 RVA: 0x0028DFA8 File Offset: 0x0028C3A8
		private static int GetMatchedIngredientSum(Receipe validRecipe, HashSet<ReceipeIngredient> suppliedIngredients)
		{
			int num = 0;
			if (validRecipe == null || suppliedIngredients == null)
			{
				return num;
			}
			foreach (ReceipeIngredient receipeIngredient in validRecipe._ingredients)
			{
				foreach (ReceipeIngredient receipeIngredient2 in suppliedIngredients)
				{
					if (receipeIngredient2._itemID == receipeIngredient._itemID)
					{
						int num2 = Mathf.Max(receipeIngredient._amount, 1);
						num += Mathf.Min(num2, receipeIngredient2._amount);
					}
				}
			}
			return num;
		}

		// Token: 0x060054A7 RID: 21671 RVA: 0x0028E060 File Offset: 0x0028C460
		private void CheckForValidUpgrade(bool skipUpgrade2DFillingCog = false)
		{
			Receipe receipe = null;
			if (this._ingredients.Count > 0)
			{
				IOrderedEnumerable<Receipe> orderedEnumerable = from ar in this._receipeBook.AvailableUpgradeCache
					where this._ingredients.All(new Func<ReceipeIngredient, bool>(ar.HasIngredient))
					where this.CanCarryUpgradeProduct(ar)
					orderby ar._ingredients.Length
					select ar;
				receipe = orderedEnumerable.FirstOrDefault<Receipe>();
			}
			if (receipe != null)
			{
				if (!this.CanCarryProduct(receipe) && receipe._ingredients[0]._itemID != receipe._productItemID)
				{
					this._validRecipeFull = true;
					receipe = null;
				}
				else
				{
					IEnumerable<ReceipeIngredient> enumerable = from vri in receipe._ingredients
						join i in this._ingredients on vri._itemID equals i._itemID
						select i;
					ReceipeIngredient[] array = enumerable.ToArray<ReceipeIngredient>();
					if (array.Length > 0)
					{
						this._upgradeCount = ItemDatabase.ItemById(receipe._productItemID)._maxUpgradesAmount;
						int itemID = receipe._ingredients[1]._itemID;
						if (this._upgradeCog.SupportedItemsCache.ContainsKey(itemID) && this._upgradeCog.SupportedItemsCache[itemID]._pattern != UpgradeCog.Patterns.NoView)
						{
							this._upgradeCount -= LocalPlayer.Inventory.GetAmountOfUpgrades(receipe._productItemID);
						}
						bool flag = false;
						bool flag2 = this._upgradeCount == 0;
						int num = 0;
						int num2 = receipe._ingredients.Sum((ReceipeIngredient i) => (i._amount != 0) ? i._amount : 1);
						for (int j = 0; j < receipe._ingredients.Length; j++)
						{
							ReceipeIngredient receipeIngredient = receipe._ingredients[j];
							int num3 = 0;
							while (num3 < array.Length && array[num3]._itemID != receipeIngredient._itemID)
							{
								num3++;
							}
							if (num3 >= array.Length)
							{
								flag = true;
							}
							else if (receipeIngredient._amount > 0)
							{
								int num4 = array[num3]._amount / receipeIngredient._amount;
								if (j > 0 && num4 < this._upgradeCount)
								{
									this._upgradeCount = num4;
								}
								num += Mathf.Min(array[num3]._amount, receipeIngredient._amount);
							}
							else
							{
								this._upgradeCount = 1;
								num++;
								flag2 = true;
							}
						}
						if (!skipUpgrade2DFillingCog)
						{
							float num5 = ((!receipe.CanCarryProduct) ? 0f : ((float)num / (float)num2));
							if (num5 > this._validRecipeFill)
							{
								this._validRecipeFill = num5;
							}
						}
						if ((this._upgradeCount <= 0 && !flag2) || flag)
						{
							receipe = null;
						}
						else
						{
							this._validRecipeFull = false;
						}
					}
					else
					{
						receipe = null;
					}
				}
			}
			bool flag3 = receipe != null;
			if (flag3)
			{
				this._validRecipe = receipe;
				Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(false);
				if (receipe._weaponStatUpgrades.Length == 0)
				{
					Scene.HudGui.ShowValidCraftingRecipes(null);
					Scene.HudGui.ShowUpgradesDistribution(this._validRecipe._productItemID, this._validRecipe._ingredients[1]._itemID, this._upgradeCount);
				}
			}
			else
			{
				if (!skipUpgrade2DFillingCog)
				{
					Scene.HudGui.CraftingReceipeBacking.gameObject.SetActive(this._validRecipeFill > 0f);
					Scene.HudGui.CraftingReceipeProgress.fillAmount = this._validRecipeFill;
				}
				Scene.HudGui.HideUpgradesDistribution();
				this._upgradeCount = 0;
			}
			if (this._targetCogRenderer.enabled != this._upgradeCount > 0)
			{
				if (!this._targetCogRenderer.enabled)
				{
					this._craftSfx2Emitter.Play();
				}
				this.ShowCogRenderer(this._upgradeCount > 0);
			}
		}

		// Token: 0x060054A8 RID: 21672 RVA: 0x0028E47C File Offset: 0x0028C87C
		private void ApplyUpgrade(Receipe craftedRecipe, ItemProperties lastIngredientProperties, int upgradeCount)
		{
			bool flag = this._itemViewsCache[craftedRecipe._productItemID]._held && !this._itemViewsCache[craftedRecipe._productItemID]._held.activeInHierarchy;
			if (flag)
			{
				if (this._itemViewsCache[craftedRecipe._productItemID]._heldWeaponInfo)
				{
					this._itemViewsCache[craftedRecipe._productItemID]._heldWeaponInfo.enabled = false;
				}
				this._itemViewsCache[craftedRecipe._productItemID]._held.SetActive(true);
			}
			if (!this.ApplyWeaponStatsUpgrades(craftedRecipe._productItemID, craftedRecipe._ingredients[1]._itemID, craftedRecipe._weaponStatUpgrades, craftedRecipe._batchUpgrade, upgradeCount, lastIngredientProperties))
			{
				this._upgradeCog.ApplyUpgradeRecipe(this._itemViewsCache[craftedRecipe._productItemID], craftedRecipe, upgradeCount);
			}
			if (flag)
			{
				this._itemViewsCache[craftedRecipe._productItemID]._held.SetActive(false);
				if (this._itemViewsCache[craftedRecipe._productItemID]._heldWeaponInfo)
				{
					this._itemViewsCache[craftedRecipe._productItemID]._heldWeaponInfo.enabled = true;
				}
			}
		}

		// Token: 0x060054A9 RID: 21673 RVA: 0x0028E5D4 File Offset: 0x0028C9D4
		public WeaponStatUpgrade[] GetWeaponStatUpgradeForIngredient(int ingredientId)
		{
			UpgradeCogItems upgradeCogItems;
			if (this._upgradeCog.SupportedItemsCache.TryGetValue(ingredientId, out upgradeCogItems))
			{
				return upgradeCogItems._weaponStatUpgrades;
			}
			return null;
		}

		// Token: 0x060054AA RID: 21674 RVA: 0x0028E604 File Offset: 0x0028CA04
		public bool ApplyWeaponStatsUpgrades(int productItemId, int ingredientItemId, WeaponStatUpgrade[] bonuses, bool batched, int upgradeCount, ItemProperties lastIngredientProperties = null)
		{
			InventoryItemView inventoryItemView = this._itemViewsCache[productItemId];
			bool flag = false;
			int i = 0;
			while (i < bonuses.Length)
			{
				switch (bonuses[i]._type)
				{
				case WeaponStatUpgrade.Types.BurningWeapon:
				{
					BurnableCloth componentInChildren = inventoryItemView._held.GetComponentInChildren<BurnableCloth>();
					if (componentInChildren)
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.BurningWeapon;
						componentInChildren.EnableBurnableCloth();
					}
					flag = true;
					break;
				}
				case WeaponStatUpgrade.Types.StickyProjectile:
					if (batched && inventoryItemView._allowMultiView)
					{
						inventoryItemView.SetMultiviewsBonus(WeaponStatUpgrade.Types.StickyProjectile);
					}
					else
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.StickyProjectile;
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.WalkmanTrack:
					WalkmanControler.LoadCassette(ingredientItemId);
					flag = true;
					break;
				case WeaponStatUpgrade.Types.BurningAmmo:
					if (batched && inventoryItemView._allowMultiView)
					{
						inventoryItemView.SetMultiviewsBonus(WeaponStatUpgrade.Types.BurningAmmo);
					}
					else
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.BurningAmmo;
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.Paint_Green:
				{
					EquipmentPainting componentInChildren2 = inventoryItemView._held.GetComponentInChildren<EquipmentPainting>();
					if (componentInChildren2)
					{
						componentInChildren2.PaintInGreen();
					}
					flag = true;
					break;
				}
				case WeaponStatUpgrade.Types.Paint_Orange:
				{
					EquipmentPainting componentInChildren3 = inventoryItemView._held.GetComponentInChildren<EquipmentPainting>();
					if (componentInChildren3)
					{
						componentInChildren3.PaintInOrange();
					}
					flag = true;
					break;
				}
				case WeaponStatUpgrade.Types.DirtyWater:
				case WeaponStatUpgrade.Types.CleanWater:
				case WeaponStatUpgrade.Types.Cooked:
				case WeaponStatUpgrade.Types.blockStaminaDrain:
				case WeaponStatUpgrade.Types.RawFood:
				case WeaponStatUpgrade.Types.DriedFood:
					goto IL_03C7;
				case WeaponStatUpgrade.Types.ItemPart:
				{
					IItemPartInventoryView itemPartInventoryView = (IItemPartInventoryView)inventoryItemView;
					itemPartInventoryView.AddPiece(Mathf.RoundToInt(bonuses[i]._amount), true);
					flag = true;
					break;
				}
				case WeaponStatUpgrade.Types.BatteryCharge:
					LocalPlayer.Stats.BatteryCharge = Mathf.Clamp(LocalPlayer.Stats.BatteryCharge + bonuses[i]._amount, 0f, 100f);
					flag = true;
					break;
				case WeaponStatUpgrade.Types.FlareGunAmmo:
					LocalPlayer.Inventory.AddItem(ItemDatabase.ItemByName("FlareGunAmmo")._id, Mathf.RoundToInt(bonuses[i]._amount * (float)upgradeCount), false, false, null);
					flag = true;
					break;
				case WeaponStatUpgrade.Types.SetWeaponAmmoBonus:
					inventoryItemView.Properties.Copy((lastIngredientProperties != ItemProperties.Any) ? lastIngredientProperties : this._itemViewsCache[ingredientItemId].Properties);
					LocalPlayer.Inventory.SortInventoryViewsByBonus(LocalPlayer.Inventory.InventoryItemViewsCache[ingredientItemId][0], inventoryItemView.ActiveBonus, false);
					flag = true;
					break;
				case WeaponStatUpgrade.Types.PoisonnedAmmo:
					if (batched && inventoryItemView._allowMultiView)
					{
						inventoryItemView.SetMultiviewsBonus(WeaponStatUpgrade.Types.PoisonnedAmmo);
					}
					else
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.PoisonnedAmmo;
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.BurningWeaponExtra:
				{
					BurnableCloth componentInChildren4 = inventoryItemView._held.GetComponentInChildren<BurnableCloth>();
					if (componentInChildren4 && inventoryItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningWeapon)
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.BurningWeaponExtra;
						componentInChildren4.EnableBurnableClothExtra();
					}
					flag = true;
					break;
				}
				case WeaponStatUpgrade.Types.Incendiary:
					inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.Incendiary;
					flag = true;
					break;
				case WeaponStatUpgrade.Types.BoneAmmo:
					if (batched && inventoryItemView._allowMultiView)
					{
						inventoryItemView.SetMultiviewsBonus(WeaponStatUpgrade.Types.BoneAmmo);
					}
					else
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.BoneAmmo;
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.CamCorderTape:
					CamCorderControler.LoadTape(ingredientItemId);
					flag = true;
					break;
				case WeaponStatUpgrade.Types.PoisonnedWeapon:
					if (inventoryItemView._heldWeaponInfo.bonus)
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.PoisonnedWeapon;
						inventoryItemView._heldWeaponInfo.bonus._bonusType = WeaponBonus.BonusTypes.Poison;
						inventoryItemView._heldWeaponInfo.bonus.enabled = true;
						RandomWeaponUpgradeVisual component = inventoryItemView._heldWeaponInfo.bonus.GetComponent<RandomWeaponUpgradeVisual>();
						if (component)
						{
							component.OnEnable();
						}
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.ModernAmmo:
					if (batched && inventoryItemView._allowMultiView)
					{
						inventoryItemView.SetMultiviewsBonus(WeaponStatUpgrade.Types.ModernAmmo);
					}
					else
					{
						inventoryItemView.ActiveBonus = WeaponStatUpgrade.Types.ModernAmmo;
					}
					flag = true;
					break;
				case WeaponStatUpgrade.Types.TapedLight:
					LocalPlayer.Inventory.AddItem(Mathf.RoundToInt(bonuses[i]._amount), 1, false, false, null);
					flag = true;
					break;
				default:
					goto IL_03C7;
				}
				IL_0410:
				i++;
				continue;
				IL_03C7:
				flag = this.ApplyWeaponBonus(bonuses[i], productItemId, ingredientItemId, upgradeCount);
				if (flag)
				{
					GameStats.UpgradesAdded.Invoke(upgradeCount);
				}
				else
				{
					Debug.LogError("Attempting to upgrade " + inventoryItemView.ItemCache._name + " which doesn't reference its weaponInfo component.");
				}
				goto IL_0410;
			}
			return flag;
		}

		// Token: 0x060054AB RID: 21675 RVA: 0x0028EA30 File Offset: 0x0028CE30
		public bool ApplyWeaponBonus(WeaponStatUpgrade bonus, int weaponItemId, int upgradeItemId, int upgradeCount)
		{
			weaponInfo heldWeaponInfo = this._itemViewsCache[weaponItemId]._heldWeaponInfo;
			if (heldWeaponInfo)
			{
				FieldInfo field = typeof(weaponInfo).GetField(bonus._type.ToString(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				float upgradeBonusAmount = this.GetUpgradeBonusAmount(weaponItemId, upgradeItemId, bonus, upgradeCount);
				if (field.FieldType == typeof(float))
				{
					field.SetValue(heldWeaponInfo, (float)field.GetValue(heldWeaponInfo) + upgradeBonusAmount);
				}
				else if (field.FieldType == typeof(int))
				{
					field.SetValue(heldWeaponInfo, (int)field.GetValue(heldWeaponInfo) + Mathf.RoundToInt(upgradeBonusAmount));
				}
				return true;
			}
			return false;
		}

		// Token: 0x060054AC RID: 21676 RVA: 0x0028EAF4 File Offset: 0x0028CEF4
		public float GetUpgradeBonusAmount(int productItemId, int ingredientItemId, WeaponStatUpgrade bonus, int upgradeCount)
		{
			AnimationCurve bonusDecayCurve = this._upgradeCog.SupportedItemsCache[ingredientItemId]._bonusDecayCurve;
			float num = 0f;
			if (bonusDecayCurve == null)
			{
				num = bonus._amount * (float)upgradeCount;
			}
			else
			{
				float num2 = (float)LocalPlayer.Inventory.GetAmountOfUpgrades(productItemId, ingredientItemId);
				float num3 = (float)ItemDatabase.ItemById(productItemId)._maxUpgradesAmount;
				for (int i = 0; i < upgradeCount; i++)
				{
					num += bonus._amount * bonusDecayCurve.Evaluate((num2 + (float)i) / num3);
				}
			}
			return num;
		}

		// Token: 0x060054AD RID: 21677 RVA: 0x0028EB80 File Offset: 0x0028CF80
		private ReceipeIngredient TryGetIngredient(int itemId)
		{
			foreach (ReceipeIngredient receipeIngredient in this._ingredients)
			{
				if (receipeIngredient._itemID == itemId)
				{
					return receipeIngredient;
				}
			}
			return null;
		}

		// Token: 0x060054AE RID: 21678 RVA: 0x0028EBEC File Offset: 0x0028CFEC
		private void ToggleItemInventoryView(int itemId, ItemProperties properties = null)
		{
			ReceipeIngredient receipeIngredient = this.TryGetIngredient(itemId);
			int num = ((receipeIngredient == null) ? 0 : receipeIngredient._amount);
			bool flag = num > 0;
			if (this.InItemViewsCache(itemId))
			{
				if (this._itemViewsCache[itemId]._allowMultiView)
				{
					if (properties == ItemProperties.Any)
					{
						this._itemViewsCache[itemId].SetMultiViewAmount(num, properties);
					}
					else
					{
						this._itemViewsCache[itemId].SetMultiViewAmount(num - this._itemViewsCache[itemId].AmountOfMultiviewWithoutProperties(itemId, properties), properties);
					}
				}
				else
				{
					if (properties != ItemProperties.Any)
					{
						this._itemViewsCache[itemId].Properties.Copy(properties);
					}
					if (this._itemViewsCache[itemId].gameObject.activeSelf != flag)
					{
						this._itemViewsCache[itemId].gameObject.SetActive(flag);
					}
				}
			}
			else if (LocalPlayer.Inventory.InventoryItemViewsCache.ContainsKey(itemId))
			{
				this._lambdaMultiView.SetAnyMultiViewAmount(LocalPlayer.Inventory.InventoryItemViewsCache[itemId][0], this._lambdaMultiView.transform, num, properties, false);
			}
			this.SelectItemViewProxyTarget();
		}

		// Token: 0x060054AF RID: 21679 RVA: 0x0028ED30 File Offset: 0x0028D130
		private void SelectItemViewProxyTarget()
		{
			if (this._ingredients.Count == 1)
			{
				InventoryItemView itemView = this.GetItemView(this._ingredients.First<ReceipeIngredient>()._itemID);
				this._completedItemViewProxy._targetView = itemView;
			}
			else
			{
				this._completedItemViewProxy.Unset();
			}
		}

		// Token: 0x060054B0 RID: 21680 RVA: 0x0028ED84 File Offset: 0x0028D184
		private InventoryItemView GetItemView(int itemId)
		{
			if (this.InItemViewsCache(itemId))
			{
				if (this._itemViewsCache[itemId]._allowMultiView)
				{
					return this._itemViewsCache[itemId].GetFirstView();
				}
				return this._itemViewsCache[itemId];
			}
			else
			{
				if (LocalPlayer.Inventory.InventoryItemViewsCache.ContainsKey(itemId))
				{
					return this._lambdaMultiView.GetFirstViewForItem(itemId);
				}
				return null;
			}
		}

		// Token: 0x060054B1 RID: 21681 RVA: 0x0028EDF8 File Offset: 0x0028D1F8
		private void IngredientCleanUp()
		{
			this._ingredients.Clear();
			IEnumerable<Item> enumerable = ItemDatabase.ItemsByType(Item.Types.CraftingMaterial | Item.Types.Craftable | Item.Types.Edible);
			foreach (Item item in enumerable)
			{
				this.ToggleItemInventoryView(item._id, ItemProperties.Any);
			}
			this._lambdaMultiView.ClearMultiViews();
		}

		// Token: 0x060054B2 RID: 21682 RVA: 0x0028EE78 File Offset: 0x0028D278
		private void EnableInventorySnapshot()
		{
			if (FMOD_StudioSystem.instance && this.InventorySnapShotInstance == null)
			{
				this.InventorySnapShotInstance = FMOD_StudioSystem.instance.GetEvent("snapshot:/inventory");
				if (this.InventorySnapShotInstance != null)
				{
					UnityUtil.ERRCHECK(this.InventorySnapShotInstance.start());
				}
			}
		}

		// Token: 0x060054B3 RID: 21683 RVA: 0x0028EEDC File Offset: 0x0028D2DC
		private void DisableInventorySnapshot()
		{
			if (this.InventorySnapShotInstance != null && this.InventorySnapShotInstance.isValid())
			{
				UnityUtil.ERRCHECK(this.InventorySnapShotInstance.stop(STOP_MODE.IMMEDIATE));
				UnityUtil.ERRCHECK(this.InventorySnapShotInstance.release());
				this.InventorySnapShotInstance = null;
			}
		}

		// Token: 0x060054B4 RID: 21684 RVA: 0x0028EF34 File Offset: 0x0028D334
		private IEnumerator animateCraftedItemRoutine(Transform tr)
		{
			Transform currentParent = tr.parent;
			Vector3 currentPos = tr.localPosition;
			Quaternion currentRot = tr.localRotation;
			tr.parent = this._craftAnimateParent;
			this._craftAnim.SetTrigger("craftTrigger");
			yield return YieldPresets.WaitOnePointThreeSeconds;
			tr.parent = currentParent;
			tr.localPosition = currentPos;
			tr.localRotation = currentRot;
			yield break;
		}

		// Token: 0x17000884 RID: 2180
		// (get) Token: 0x060054B5 RID: 21685 RVA: 0x0028EF56 File Offset: 0x0028D356
		public HashSet<ReceipeIngredient> Ingredients
		{
			get
			{
				return this._ingredients;
			}
		}

		// Token: 0x17000885 RID: 2181
		// (get) Token: 0x060054B6 RID: 21686 RVA: 0x0028EF5E File Offset: 0x0028D35E
		public float RecipeFill
		{
			get
			{
				return this._validRecipeFill;
			}
		}

		// Token: 0x17000886 RID: 2182
		// (get) Token: 0x060054B7 RID: 21687 RVA: 0x0028EF66 File Offset: 0x0028D366
		public bool RecipeProductFull
		{
			get
			{
				return this._validRecipeFull;
			}
		}

		// Token: 0x17000887 RID: 2183
		// (get) Token: 0x060054B8 RID: 21688 RVA: 0x0028EF6E File Offset: 0x0028D36E
		public bool CanStore
		{
			get
			{
				return this.CheckStorage() && this._ingredients.Count > 0;
			}
		}

		// Token: 0x17000888 RID: 2184
		// (get) Token: 0x060054B9 RID: 21689 RVA: 0x0028EF8C File Offset: 0x0028D38C
		public bool CanCraft
		{
			get
			{
				return this._validRecipe != null && !this._validRecipeFull && Mathf.Approximately(this._validRecipeFill, 1f);
			}
		}

		// Token: 0x17000889 RID: 2185
		// (get) Token: 0x060054BA RID: 21690 RVA: 0x0028EFB7 File Offset: 0x0028D3B7
		public Dictionary<int, InventoryItemView> ItemViewsCache
		{
			get
			{
				return this._itemViewsCache;
			}
		}

		// Token: 0x1700088A RID: 2186
		// (get) Token: 0x060054BB RID: 21691 RVA: 0x0028EFBF File Offset: 0x0028D3BF
		public Dictionary<int, InventoryItemView> ItemExtensionViewsCache
		{
			get
			{
				return this._itemExtensionViewsCache;
			}
		}

		// Token: 0x1700088B RID: 2187
		// (get) Token: 0x060054BC RID: 21692 RVA: 0x0028EFC7 File Offset: 0x0028D3C7
		// (set) Token: 0x060054BD RID: 21693 RVA: 0x0028EFCF File Offset: 0x0028D3CF
		public ItemStorage Storage { get; set; }

		// Token: 0x1700088C RID: 2188
		// (get) Token: 0x060054BE RID: 21694 RVA: 0x0028EFD8 File Offset: 0x0028D3D8
		// (set) Token: 0x060054BF RID: 21695 RVA: 0x0028EFE0 File Offset: 0x0028D3E0
		public ICraftOverride CraftOverride { get; set; }

		// Token: 0x1700088D RID: 2189
		// (get) Token: 0x060054C0 RID: 21696 RVA: 0x0028EFE9 File Offset: 0x0028D3E9
		public bool HasValideRecipe
		{
			get
			{
				return this._validRecipe != null;
			}
		}

		// Token: 0x04005AC3 RID: 23235
		public bool _legacyCraftingSystem;

		// Token: 0x04005AC4 RID: 23236
		[EnumFlags]
		public Item.Types _acceptedTypes;

		// Token: 0x04005AC5 RID: 23237
		public ReceipeBook _receipeBook;

		// Token: 0x04005AC6 RID: 23238
		public PlayerInventory _inventory;

		// Token: 0x04005AC7 RID: 23239
		public Material _selectedMaterial;

		// Token: 0x04005AC8 RID: 23240
		public MeshRenderer _targetCogRenderer;

		// Token: 0x04005AC9 RID: 23241
		public GameObject _craftSfx;

		// Token: 0x04005ACA RID: 23242
		public GameObject _craftSfx2;

		// Token: 0x04005ACB RID: 23243
		public GameObject _craftParticle1;

		// Token: 0x04005ACC RID: 23244
		public Transform _craftParticleSpawnPos;

		// Token: 0x04005ACD RID: 23245
		public Animator _craftAnim;

		// Token: 0x04005ACE RID: 23246
		public Transform _craftAnimateParent;

		// Token: 0x04005ACF RID: 23247
		public InventoryItemView[] _itemViews;

		// Token: 0x04005AD0 RID: 23248
		public UpgradeCog _upgradeCog;

		// Token: 0x04005AD1 RID: 23249
		public InventoryItemViewProxy _completedItemViewProxy;

		// Token: 0x04005AD2 RID: 23250
		private GameObject _clickToCombineButton;

		// Token: 0x04005AD3 RID: 23251
		private Material _normalMaterial;

		// Token: 0x04005AD4 RID: 23252
		private HashSet<ReceipeIngredient> _ingredients;

		// Token: 0x04005AD5 RID: 23253
		private Dictionary<int, InventoryItemView> _itemViewsCache;

		// Token: 0x04005AD6 RID: 23254
		private Dictionary<int, InventoryItemView> _itemExtensionViewsCache;

		// Token: 0x04005AD7 RID: 23255
		private bool _validRecipeFull;

		// Token: 0x04005AD8 RID: 23256
		private float _validRecipeFill;

		// Token: 0x04005AD9 RID: 23257
		private Receipe _validRecipe;

		// Token: 0x04005ADA RID: 23258
		private int _upgradeCount;

		// Token: 0x04005ADB RID: 23259
		private FMOD_StudioEventEmitter _craftSfxEmitter;

		// Token: 0x04005ADC RID: 23260
		private FMOD_StudioEventEmitter _craftSfx2Emitter;

		// Token: 0x04005ADD RID: 23261
		private bool _initialized;

		// Token: 0x04005ADE RID: 23262
		private bool _hovered;

		// Token: 0x04005ADF RID: 23263
		private InventoryItemView _lambdaMultiView;

		// Token: 0x04005AE0 RID: 23264
		private float _targetCogRotate;

		// Token: 0x04005AE1 RID: 23265
		private EventInstance InventorySnapShotInstance;

		// Token: 0x04005AE2 RID: 23266
		private EventInstance CogRotateEventInstance;

		// Token: 0x04005AE3 RID: 23267
		private CueInstance CogRotateEventKeyoff;

		// Token: 0x02000C75 RID: 3189
		private class CompareRecipeProduct : IEqualityComparer<Receipe>
		{
			// Token: 0x060054E8 RID: 21736 RVA: 0x0028F260 File Offset: 0x0028D660
			public bool Equals(Receipe x, Receipe y)
			{
				if (x._id != y._id)
				{
					if (x._type == Receipe.Types.Extension && y._type == Receipe.Types.Extension && x._ingredients.Length == y._ingredients.Length)
					{
						for (int i = 1; i < x._ingredients.Length; i++)
						{
							if (x._ingredients[i]._itemID != y._ingredients[i]._itemID)
							{
								return false;
							}
						}
					}
					else
					{
						if (x._productItemAmount._min != y._productItemAmount._min || x._productItemAmount._max != y._productItemAmount._max || x._weaponStatUpgrades.Length != y._weaponStatUpgrades.Length)
						{
							return false;
						}
						if (x._weaponStatUpgrades.Length > 0)
						{
							for (int j = 0; j < x._weaponStatUpgrades.Length; j++)
							{
								if (x._weaponStatUpgrades[j]._type != y._weaponStatUpgrades[j]._type)
								{
									return false;
								}
							}
						}
					}
				}
				return true;
			}

			// Token: 0x060054E9 RID: 21737 RVA: 0x0028F388 File Offset: 0x0028D788
			public int GetHashCode(Receipe obj)
			{
				if (obj._type == Receipe.Types.Upgrade)
				{
					if (obj._weaponStatUpgrades.Length > 0)
					{
						return (int)(obj._weaponStatUpgrades[0]._type + (int)(obj._type * (Receipe.Types)10000));
					}
					if (obj._ingredients.Length == 3)
					{
						return (int)(obj._ingredients[1]._itemID * 1000 + obj._ingredients[2]._itemID * 500000 + obj._type * (Receipe.Types)10000000);
					}
					if (obj._ingredients.Length == 2)
					{
						return (int)(obj._ingredients[1]._itemID * 1000 + obj._type * (Receipe.Types)10000000);
					}
				}
				return (int)(obj._productItemID + obj._type * (Receipe.Types)100000);
			}
		}
	}
}
