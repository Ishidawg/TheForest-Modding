using System;
using System.Collections;
using System.Collections.Generic;
using Bolt;
using FMOD.Studio;
using TheForest.Items.Core;
using TheForest.Items.Craft;
using TheForest.Items.Special;
using TheForest.Items.Utils;
using TheForest.Items.World;
using TheForest.Save;
using TheForest.Tools;
using TheForest.UI;
using TheForest.Utils;
using UniLinq;
using UnityEngine;
using UnityEngine.Events;

namespace TheForest.Items.Inventory
{
	// Token: 0x02000CA9 RID: 3241
	[AddComponentMenu("Items/Inventory/Player Inventory")]
	[DoNotSerializePublic]
	public class PlayerInventory : MonoBehaviour
	{
		// Token: 0x06005618 RID: 22040 RVA: 0x00296FA8 File Offset: 0x002953A8
		private void Awake()
		{
			this.SpecialItemsControlers = new Dictionary<int, SpecialItemControlerBase>();
			this._equipmentSlots = new InventoryItemView[Enum.GetValues(typeof(Item.EquipmentSlot)).Length];
			this._equipmentSlotsPrevious = new InventoryItemView[this._equipmentSlots.Length];
			this._equipmentSlotsPreviousOverride = new InventoryItemView[this._equipmentSlots.Length];
			this._equipmentSlotsNext = new InventoryItemView[this._equipmentSlots.Length];
			this._equipmentSlotsLocked = new bool[this._equipmentSlots.Length];
			this._equipPreviousTime = float.MaxValue;
			this._noEquipedItem = this._inventoryGO.AddComponent<InventoryItemView>();
			this._noEquipedItem.enabled = false;
			this._noEquipedItem._itemId = -1;
			for (int i = 0; i < this._equipmentSlots.Length; i++)
			{
				this._equipmentSlots[i] = this._noEquipedItem;
				this._equipmentSlotsPrevious[i] = this._noEquipedItem;
				this._equipmentSlotsPreviousOverride[i] = this._noEquipedItem;
				this._equipmentSlotsNext[i] = this._noEquipedItem;
			}
			this._quickSelectItemIds = new int[4];
			EventRegistry.Player.Subscribe(TfEvent.EquippedItem, new EventRegistry.SubscriberCallback(this.CheckQuickSelectAutoAdd));
			EventRegistry.Player.Subscribe(TfEvent.AddedItem, new EventRegistry.SubscriberCallback(this.CheckQuickSelectAutoAdd));
			this._itemAnimHash = new ItemAnimatorHashHelper();
			this.InitItemCache();
			if (!LevelSerializer.IsDeserializing)
			{
				foreach (QuickSelectViews quickSelectViews2 in LocalPlayer.QuickSelectViews)
				{
					quickSelectViews2.Awake();
				}
			}
		}

		// Token: 0x06005619 RID: 22041 RVA: 0x00297134 File Offset: 0x00295534
		public void Start()
		{
			if (ForestVR.Prototype)
			{
				base.enabled = false;
				return;
			}
			this._craftingCog._inventory = this;
			for (int i = 0; i < this._itemViews.Length; i++)
			{
				if (this._itemViews[i])
				{
					this._itemViews[i].Init();
				}
			}
			this._pm = base.GetComponentInChildren<playerScriptSetup>().pmControl;
			this._inventoryGO.SetActive(false);
			this._inventoryGO.transform.parent = null;
			this._inventoryGO.transform.eulerAngles = new Vector3(0f, this._inventoryGO.transform.eulerAngles.y, 0f);
			if (!LevelSerializer.IsDeserializing && !PlayerSpawn.LoadingSavedCharacter)
			{
				for (int j = 0; j < ItemDatabase.Items.Length; j++)
				{
					Item item = ItemDatabase.Items[j];
					this.ToggleInventoryItemView(item._id, false, null);
				}
				DecayingInventoryItemView.LastUsed = null;
			}
			else
			{
				base.enabled = false;
			}
		}

		// Token: 0x0600561A RID: 22042 RVA: 0x00297250 File Offset: 0x00295650
		public IEnumerator OnDeserialized()
		{
			if (CoopPeerStarter.DedicatedHost)
			{
				global::UnityEngine.Object.Destroy(base.gameObject);
				yield break;
			}
			this._possessedItems.RemoveRange(this._possessedItemsCount, this._possessedItems.Count - this._possessedItemsCount);
			this._possessedItemCache = this._possessedItems.ToDictionary((InventoryItem i) => i._itemId);
			yield return YieldPresets.WaitOnePointFiveSeconds;
			try
			{
				try
				{
					foreach (KeyValuePair<int, List<InventoryItemView>> keyValuePair in this._inventoryItemViewsCache)
					{
						for (int i2 = keyValuePair.Value.Count<InventoryItemView>() - 1; i2 >= 0; i2--)
						{
							if (!keyValuePair.Value[i2])
							{
								keyValuePair.Value.RemoveAt(i2);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
				try
				{
					if (!this.SpecialItemsControlers.ContainsKey(ItemDatabase.ItemByName("Compass")._id))
					{
						Debug.LogError("Adding compass controller");
						CompassControler compassControler = this._specialItems.AddComponent<CompassControler>();
						compassControler._itemId = ItemDatabase.ItemByName("Compass")._id;
						compassControler._button = SpecialItemControlerBase.Buttons.Utility;
					}
					if (!this.SpecialItemsControlers.ContainsKey(ItemDatabase.ItemByName("MetalTinTray")._id))
					{
						Debug.LogError("Adding metal tin tray controller");
						MetalTinTrayControler metalTinTrayControler = this._specialItems.AddComponent<MetalTinTrayControler>();
						metalTinTrayControler._itemId = ItemDatabase.ItemByName("MetalTinTray")._id;
						metalTinTrayControler._storage = this._specialItems.transform.Find("MetalTinTrayStorage").GetComponent<ItemStorage>();
					}
				}
				catch (Exception ex2)
				{
					Debug.LogException(ex2);
				}
				this.FixMaxAmountBonuses();
				try
				{
					if (!this._possessedItemCache.ContainsKey(this.DefaultLight._itemId) && !this._equipmentSlotsIds.Contains(this.DefaultLight._itemId))
					{
						this.AddItem(this.DefaultLight._itemId, 1, true, false, null);
					}
				}
				catch (Exception ex3)
				{
					Debug.LogException(ex3);
				}
				try
				{
					if (!this._possessedItemCache.ContainsKey(this._defaultWeaponItemId) && !this._equipmentSlotsIds.Contains(this._defaultWeaponItemId))
					{
						this.AddItem(this._defaultWeaponItemId, 1, true, false, null);
					}
				}
				catch (Exception ex4)
				{
					Debug.LogException(ex4);
				}
				try
				{
					if (!LocalPlayer.SavedData.ExitedEndgame)
					{
						int timmyPhotoItemId = ItemDatabase.ItemByName("PhotoTimmy")._id;
						if (!this._possessedItems.Any((InventoryItem pi) => pi._itemId == timmyPhotoItemId) && !this._equipmentSlotsIds.Contains(timmyPhotoItemId))
						{
							Debug.LogError("Adding timmy photo failsafe");
							this.AddItem(timmyPhotoItemId, 1, true, true, null);
						}
					}
				}
				catch (Exception ex5)
				{
					Debug.LogException(ex5);
				}
				try
				{
					this._craftingCog.OnDeserialized();
					this._craftingCog.GetComponent<UpgradeCog>().Awake();
				}
				catch (Exception ex6)
				{
					Debug.LogException(ex6);
				}
				try
				{
					for (int j = 0; j < ItemDatabase.Items.Length; j++)
					{
						Item item = ItemDatabase.Items[j];
						try
						{
							this.ToggleInventoryItemView(item._id, true, ItemProperties.Any);
							bool flag = this._possessedItemCache.ContainsKey(item._id);
							if (item.MatchType(Item.Types.Special) && (flag || this._equipmentSlotsIds.Contains(item._id)))
							{
								this._specialItems.SendMessage("PickedUpSpecialItem", item._id);
							}
							if (flag)
							{
								this._possessedItemCache[item._id]._maxAmount = ((item._maxAmount != 0) ? item._maxAmount : int.MaxValue);
								if (this._possessedItemCache[item._id]._amount > this._possessedItemCache[item._id].MaxAmount)
								{
									this._possessedItemCache[item._id]._amount = this._possessedItemCache[item._id].MaxAmount;
								}
								for (int k = 0; k < this._inventoryItemViewsCache[item._id].Count; k++)
								{
									if (this._inventoryItemViewsCache[item._id][k].gameObject.activeSelf)
									{
										this._inventoryItemViewsCache[item._id][k].OnDeserialized();
									}
								}
							}
						}
						catch (Exception)
						{
						}
					}
				}
				catch (Exception ex7)
				{
					Debug.LogException(ex7);
				}
				yield return null;
				try
				{
					if (this._equipmentSlotsIds != null)
					{
						this.HideAllEquiped(false, false);
						for (int l = 0; l < this._equipmentSlotsIds.Length; l++)
						{
							if (this._equipmentSlotsIds[l] > 0)
							{
								this.LockEquipmentSlot((Item.EquipmentSlot)l);
							}
						}
						for (int m = 0; m < this._equipmentSlotsIds.Length; m++)
						{
							if (this._equipmentSlotsIds[m] > 0)
							{
								int num = this._equipmentSlotsIds[m];
								ItemUtils.ApplyEffectsToStats(this._inventoryItemViewsCache[num][0].ItemCache._equipedStatEffect, false, 1);
								this._equipmentSlots[m] = null;
								this.UnlockEquipmentSlot((Item.EquipmentSlot)m);
								if (!this.Equip(num, true))
								{
									this.AddItem(num, 1, true, true, null);
								}
							}
						}
					}
				}
				catch (Exception ex8)
				{
					Debug.LogException(ex8);
				}
				try
				{
					if (this._upgradeCounters != null)
					{
						for (int n = 0; n < this._upgradeCountersCount; n++)
						{
							PlayerInventory.SerializableItemUpgradeCounters serializableItemUpgradeCounters = this._upgradeCounters[n];
							if (!this.ItemsUpgradeCounters.ContainsKey(serializableItemUpgradeCounters._itemId))
							{
								this.ItemsUpgradeCounters[serializableItemUpgradeCounters._itemId] = new PlayerInventory.UpgradeCounterDict();
							}
							for (int num2 = 0; num2 < serializableItemUpgradeCounters._count; num2++)
							{
								PlayerInventory.SerializableUpgradeCounter serializableUpgradeCounter = serializableItemUpgradeCounters._counters[num2];
								this._craftingCog.ApplyWeaponStatsUpgrades(serializableItemUpgradeCounters._itemId, serializableUpgradeCounter._upgradeItemId, this._craftingCog._upgradeCog.SupportedItemsCache[serializableUpgradeCounter._upgradeItemId]._weaponStatUpgrades, false, serializableUpgradeCounter._amount, null);
								this.ItemsUpgradeCounters[serializableItemUpgradeCounters._itemId][serializableUpgradeCounter._upgradeItemId] = serializableUpgradeCounter._amount;
							}
						}
					}
				}
				catch (Exception ex9)
				{
					Debug.LogException(ex9);
				}
				try
				{
					for (int num3 = 0; num3 < this._upgradeViewReceivers.Count; num3++)
					{
						this._upgradeViewReceivers[num3].OnDeserialized();
					}
				}
				catch (Exception ex10)
				{
					Debug.LogException(ex10);
				}
				try
				{
					foreach (QuickSelectViews quickSelectViews2 in LocalPlayer.QuickSelectViews)
					{
						quickSelectViews2.Awake();
						quickSelectViews2.ShowLocalPlayerViews();
					}
				}
				catch (Exception ex11)
				{
					Debug.LogException(ex11);
				}
			}
			finally
			{
				base.enabled = true;
				DecayingInventoryItemView.LastUsed = null;
			}
			yield break;
		}

		// Token: 0x0600561B RID: 22043 RVA: 0x0029726C File Offset: 0x0029566C
		private void Update()
		{
			if (SteamDSConfig.isDedicatedServer)
			{
				return;
			}
			bool buttonDown = TheForest.Utils.Input.GetButtonDown("Esc");
			bool buttonDown2 = TheForest.Utils.Input.GetButtonDown("Inventory");
			if ((this.CurrentView != PlayerInventory.PlayerViews.Pause && !this.BlockTogglingInventory && !LocalPlayer.AnimControl.useRootMotion && !LocalPlayer.FpCharacter.jumping && !LocalPlayer.FpCharacter.drinking && !LocalPlayer.AnimControl.endGameCutScene && !LocalPlayer.AnimControl.blockInventoryOpen && !LocalPlayer.FpCharacter.PushingSled && !LocalPlayer.Animator.GetBool("drawBowBool") && !LocalPlayer.AnimControl.slingShotAim && buttonDown2) || (this.CurrentView == PlayerInventory.PlayerViews.Inventory && buttonDown))
			{
				Scene.HudGui.ClearMpPlayerList();
				this.ToggleInventory();
			}
			else if ((buttonDown && this.CurrentView != PlayerInventory.PlayerViews.Book && !LocalPlayer.Create.CreateMode && !LocalPlayer.PlayerDeadCam.activeSelf && !this.QuickSelectGamepadSwitch) || (this.CurrentView == PlayerInventory.PlayerViews.Pause && !Scene.HudGui.IsNull() && !Scene.HudGui.PauseMenu.IsNull() && !Scene.HudGui.PauseMenu.activeSelf))
			{
				Scene.HudGui.ClearMpPlayerList();
				this.TogglePauseMenu();
			}
			else if (this.CurrentView == PlayerInventory.PlayerViews.World)
			{
				bool flag = this.IsSlotEmpty(Item.EquipmentSlot.RightHand);
				if (TheForest.Utils.Input.GetButtonDown("Fire1"))
				{
					if (!flag && ForestVR.Enabled)
					{
						InventoryItemView inventoryItemView = this._equipmentSlots[0];
						Item itemCache = inventoryItemView.ItemCache;
						if (itemCache.MatchType(Item.Types.Droppable) && !itemCache.MatchType(Item.Types.Projectile))
						{
							if ((!ForestVR.Enabled || !itemCache._VRBlockPutAway) && !itemCache._blockPutAway && !this.BlockDrop)
							{
								this.DropEquipedWeapon(itemCache.MatchType(Item.Types.Droppable) && !this.UseAltWorldPrefab);
							}
							return;
						}
					}
					this.Attack();
				}
				if (TheForest.Utils.Input.GetButtonUp("Fire1"))
				{
					if (!ForestVR.Enabled)
					{
						this.ReleaseAttack();
					}
				}
				else if (TheForest.Utils.Input.GetButtonDown("AltFire"))
				{
					this.Block();
				}
				else if (TheForest.Utils.Input.GetButtonUp("AltFire"))
				{
					this.UnBlock();
				}
				else if (FirstPersonCharacter.GetDropInput() && !LocalPlayer.AnimControl.upsideDown && !flag)
				{
					InventoryItemView inventoryItemView2 = this._equipmentSlots[0];
					Item itemCache2 = inventoryItemView2.ItemCache;
					if ((!ForestVR.Enabled || !itemCache2._VRBlockPutAway) && !itemCache2._blockPutAway)
					{
						this.DropEquipedWeapon(itemCache2.MatchType(Item.Types.Droppable) && !this.UseAltWorldPrefab);
					}
				}
				if (this._equipPreviousTime < Time.time && !LocalPlayer.AnimControl.upsideDown && !LocalPlayer.Create.CreateMode)
				{
					this.EquipPreviousWeapon(false);
				}
				this.CheckQuickSelect();
			}
			else if (this.CurrentView == PlayerInventory.PlayerViews.ClosingInventory)
			{
				this.CurrentView = PlayerInventory.PlayerViews.World;
			}
			this.RefreshDropIcon();
		}

		// Token: 0x0600561C RID: 22044 RVA: 0x002975D8 File Offset: 0x002959D8
		private void OnDestroy()
		{
			foreach (QuickSelectViews quickSelectViews2 in LocalPlayer.QuickSelectViews)
			{
				if (quickSelectViews2)
				{
					quickSelectViews2.OnDestroy();
				}
			}
			if (this._inventoryGO != null && this._inventoryGO.transform.parent != base.transform)
			{
				global::UnityEngine.Object.Destroy(this._inventoryGO);
			}
		}

		// Token: 0x0600561D RID: 22045 RVA: 0x00297650 File Offset: 0x00295A50
		private void OnLevelWasLoaded()
		{
			if (Application.loadedLevelName == "TitleScene")
			{
				global::UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		// Token: 0x0600561E RID: 22046 RVA: 0x00297671 File Offset: 0x00295A71
		public static Vector3 SfxListenerSpacePosition(Vector3 worldPosition)
		{
			return LocalPlayer.MainCamTr.TransformPoint(LocalPlayer.InventoryCam.transform.InverseTransformPoint(worldPosition));
		}

		// Token: 0x0600561F RID: 22047 RVA: 0x00297690 File Offset: 0x00295A90
		public void ToggleInventory()
		{
			if (this.CurrentView == PlayerInventory.PlayerViews.Inventory || LocalPlayer.Stats.Dead)
			{
				this.Close();
			}
			else if (!LocalPlayer.WaterViz.InWater)
			{
				this.Open(this._craftingCog);
			}
			else
			{
				LocalPlayer.Tuts.ShowNoInventoryUnderWater();
			}
		}

		// Token: 0x06005620 RID: 22048 RVA: 0x002976F0 File Offset: 0x00295AF0
		public void Open(IItemStorage storage)
		{
			TheForest.Utils.Input.SetState(InputState.Inventory, true);
			if (this.CurrentView != PlayerInventory.PlayerViews.Inventory || this.CurrentStorage != storage)
			{
				bool flag = this.CurrentView == PlayerInventory.PlayerViews.Book;
				if (this.CurrentView == PlayerInventory.PlayerViews.Book)
				{
					if (ForestVR.Enabled)
					{
						LocalPlayer.Create.CloseTheBook(false);
						if (LocalPlayer.AnimControl.realBookGo.activeSelf)
						{
							return;
						}
					}
					else
					{
						LocalPlayer.Create.CloseBookForInventory();
						base.enabled = true;
					}
				}
				this.CurrentStorage = storage;
				this.CurrentStorage.Open();
				this.CurrentView = PlayerInventory.PlayerViews.Inventory;
				LocalPlayer.FpCharacter.LockView(true);
				if (!ForestVR.Enabled)
				{
					LocalPlayer.MainCam.enabled = false;
				}
				VirtualCursor.Instance.SetCursorType(VirtualCursor.CursorTypes.Inventory);
				Scene.HudGui.CheckHudState();
				Scene.HudGui.Grid.gameObject.SetActive(false);
				this._inventoryGO.tag = "open";
				this._inventoryGO.SetActive(true);
				if (ForestVR.Enabled)
				{
					LocalPlayer.InventoryMouseEventsVR.enabled = true;
					this.PositionInventoryVR();
				}
				else
				{
					this._inventoryGO.transform.position = new Vector3(this._inventoryGO.transform.position.x, 300f, this._inventoryGO.transform.position.z);
				}
				LocalPlayer.Sfx.PlayOpenInventory();
				this.IsOpenningInventory = true;
				base.Invoke("PauseTimeInInventory", (!flag) ? 0.05f : 0.25f);
			}
		}

		// Token: 0x06005621 RID: 22049 RVA: 0x0029788C File Offset: 0x00295C8C
		private void PositionInventoryVR()
		{
			Vector3 vector = LocalPlayer.vrAdapter.VREyeCamera.transform.forward;
			vector.y = 0f;
			vector = vector.normalized;
			float num = Vector3.Angle(vector, LocalPlayer.Transform.forward);
			if (Vector3.Cross(vector, LocalPlayer.Transform.forward).y > 0f)
			{
				num = -num;
			}
			LocalPlayer.Transform.Rotate(Vector3.up, num);
			LocalPlayer.vrPlayerControl.VROffsetTransform.Rotate(Vector3.up, -num);
			this._inventoryGO.transform.position = LocalPlayer.InventoryPositionVR.transform.position;
			this._inventoryGO.transform.rotation = LocalPlayer.InventoryPositionVR.transform.rotation;
			if (LocalPlayer.IsInCaves)
			{
				return;
			}
			if (LocalPlayer.ScriptSetup.treeHit.TerrainAngleToPlayer > 0f)
			{
				Vector3 position = LocalPlayer.InventoryPositionVR.transform.position;
				float num2 = Terrain.activeTerrain.SampleHeight(position) + Terrain.activeTerrain.transform.position.y;
				if (position.y - num2 < 2f && position.y - num2 > -2f)
				{
					float num3 = (position.x - Terrain.activeTerrain.transform.position.x) / Terrain.activeTerrain.terrainData.size.x;
					float num4 = (position.z - Terrain.activeTerrain.transform.position.z) / Terrain.activeTerrain.terrainData.size.z;
					Vector3 vector2 = Terrain.activeTerrain.terrainData.GetInterpolatedNormal(num3, num4);
					vector2 += Vector3.up;
					this._inventoryGO.transform.rotation = Quaternion.LookRotation(Vector3.Cross(LocalPlayer.Transform.right, vector2), vector2);
					Vector3 position2 = this._inventoryGO.transform.position;
					position2.y += LocalPlayer.ScriptSetup.treeHit.TerrainAngleToPlayer * 0.8f;
					this._inventoryGO.transform.position = position2;
				}
			}
		}

		// Token: 0x06005622 RID: 22050 RVA: 0x00297AE8 File Offset: 0x00295EE8
		private void PauseTimeInInventory()
		{
			if (this.CurrentView == PlayerInventory.PlayerViews.Inventory)
			{
				this.IsOpenningInventory = false;
				if (!BoltNetwork.isRunning && !GameSetup.IsHardMode && !GameSetup.IsHardSurvivalMode && !ForestVR.Enabled)
				{
					Time.timeScale = 0f;
				}
				if (!ForestVR.Enabled)
				{
					Application.targetFrameRate = 60;
				}
				this._pauseSnapshot = FMODCommon.PlayOneshot("snapshot:/Inventory Pause", Vector3.zero, new object[0]);
			}
		}

		// Token: 0x06005623 RID: 22051 RVA: 0x00297B68 File Offset: 0x00295F68
		public void Close()
		{
			if (this.CurrentView == PlayerInventory.PlayerViews.Inventory || this._inventoryGO.activeSelf)
			{
				TheForest.Utils.Input.SetState(InputState.Inventory, false);
				this.CurrentView = PlayerInventory.PlayerViews.ClosingInventory;
				this.CurrentStorage.Close();
				this.CurrentStorage = null;
				this._inventoryGO.tag = "closed";
				this._inventoryGO.SetActive(false);
				if (LocalPlayer.FakeCaveVR.gameObject.activeSelf)
				{
					LocalPlayer.FakeCaveVR.gameObject.SetActive(false);
				}
				Scene.HudGui.CheckHudState();
				Scene.HudGui.Grid.gameObject.SetActive(true);
				LocalPlayer.FpCharacter.UnLockView();
				LocalPlayer.MainCam.enabled = true;
				LocalPlayer.InventoryMouseEventsVR.enabled = false;
				PlayerPreferences.ApplyMaxFrameRate();
				VirtualCursor.Instance.SetCursorType(VirtualCursor.CursorTypes.Hand);
				if (!string.IsNullOrEmpty(this.PendingSendMessage))
				{
					base.SendMessage(this.PendingSendMessage);
					this.PendingSendMessage = null;
				}
				LocalPlayer.Sfx.PlayCloseInventory();
				Time.timeScale = 1f;
				this.IsOpenningInventory = false;
				if (this._pauseSnapshot != null && this._pauseSnapshot.isValid())
				{
					UnityUtil.ERRCHECK(this._pauseSnapshot.stop(STOP_MODE.ALLOWFADEOUT));
				}
			}
		}

		// Token: 0x06005624 RID: 22052 RVA: 0x00297CB4 File Offset: 0x002960B4
		public void TogglePauseMenu()
		{
			this.BlockTogglingInventory = false;
			bool flag = this.CurrentView == PlayerInventory.PlayerViews.Pause;
			Scene.HudGui.TogglePauseMenu(!flag);
		}

		// Token: 0x06005625 RID: 22053 RVA: 0x00297CE0 File Offset: 0x002960E0
		public void FixMaxAmountBonuses()
		{
			try
			{
				foreach (InventoryItem inventoryItem in this._possessedItemCache.Values)
				{
					Item item = ItemDatabase.ItemById(inventoryItem._itemId);
					if (inventoryItem._maxAmountBonus > 0)
					{
						inventoryItem._maxAmountBonus = 0;
					}
				}
				foreach (InventoryItem inventoryItem2 in this._possessedItemCache.Values)
				{
					Item item2 = ItemDatabase.ItemById(inventoryItem2._itemId);
					if (item2 != null && item2._ownedStatEffect != null && item2._ownedStatEffect.Length > 0)
					{
						ItemUtils.ApplyEffectsToStats(item2._ownedStatEffect, true, inventoryItem2._amount);
						inventoryItem2._amount = Mathf.Clamp(inventoryItem2._amount, 0, inventoryItem2.MaxAmount);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		// Token: 0x06005626 RID: 22054 RVA: 0x00297E20 File Offset: 0x00296220
		private void CheckQuickSelect()
		{
			if (this.CurrentView == PlayerInventory.PlayerViews.World && !LocalPlayer.AnimControl.swimming && !LocalPlayer.AnimControl.onRope && !LocalPlayer.AnimControl.onRaft && !LocalPlayer.AnimControl.sitting && !LocalPlayer.AnimControl.upsideDown && !LocalPlayer.AnimControl.skinningAnimal && !LocalPlayer.AnimControl.endGameCutScene && !LocalPlayer.AnimControl.useRootMotion && !LocalPlayer.Create.CreateMode && !LocalPlayer.AnimControl.PlayerIsAttacking())
			{
				bool flag = !TheForest.Utils.Input.IsGamePad || this.QuickSelectGamepadSwitch;
				for (int i = 0; i < this._quickSelectItemIds.Length; i++)
				{
					if (this._quickSelectItemIds[i] > 0 && flag && TheForest.Utils.Input.GetButtonDown(this._quickSelectButtons[i]) && this.Owns(this._quickSelectItemIds[i], false))
					{
						Item item = ItemDatabase.ItemById(this._quickSelectItemIds[i]);
						if (item.MatchType(Item.Types.Equipment))
						{
							if (this.Equip(this._quickSelectItemIds[i], false))
							{
								this.UnBlock();
								this.Blocking(true);
								LocalPlayer.Sfx.PlayWhoosh();
							}
						}
						else if (item.MatchType(Item.Types.Edible))
						{
							this.InventoryItemViewsCache[this._quickSelectItemIds[i]][0].UseEdible();
							foreach (QuickSelectViews quickSelectViews2 in LocalPlayer.QuickSelectViews)
							{
								quickSelectViews2.ShowLocalPlayerViews();
							}
						}
					}
				}
			}
		}

		// Token: 0x06005627 RID: 22055 RVA: 0x00297FD6 File Offset: 0x002963D6
		private void CheckQuickSelectAutoAdd(object o)
		{
			this.CheckQuickSelectAutoAdd((int)o);
		}

		// Token: 0x06005628 RID: 22056 RVA: 0x00297FE4 File Offset: 0x002963E4
		private void CheckQuickSelectAutoAdd(int itemId)
		{
			if (this.CurrentView == PlayerInventory.PlayerViews.World)
			{
				Item item = ItemDatabase.ItemById(itemId);
				if (item != null && item.MatchType(Item.Types.Edible | Item.Types.Projectile | Item.Types.RangedWeapon | Item.Types.Weapon) && item._maxAmount >= 0)
				{
					for (int i = 0; i < this._quickSelectItemIds.Length; i++)
					{
						if (this._quickSelectItemIds[i] == itemId)
						{
							return;
						}
					}
					for (int j = 0; j < this._quickSelectItemIds.Length; j++)
					{
						if (this._quickSelectItemIds[j] <= 0)
						{
							this._quickSelectItemIds[j] = itemId;
							return;
						}
					}
					EventRegistry.Player.Unsubscribe(TfEvent.EquippedItem, new EventRegistry.SubscriberCallback(this.CheckQuickSelectAutoAdd));
					EventRegistry.Player.Unsubscribe(TfEvent.AddedItem, new EventRegistry.SubscriberCallback(this.CheckQuickSelectAutoAdd));
				}
			}
		}

		// Token: 0x06005629 RID: 22057 RVA: 0x002980B5 File Offset: 0x002964B5
		public void LockEquipmentSlot(Item.EquipmentSlot slot)
		{
			this._equipmentSlotsLocked[(int)slot] = true;
		}

		// Token: 0x0600562A RID: 22058 RVA: 0x002980C0 File Offset: 0x002964C0
		public void UnlockEquipmentSlot(Item.EquipmentSlot slot)
		{
			this._equipmentSlotsLocked[(int)slot] = false;
		}

		// Token: 0x0600562B RID: 22059 RVA: 0x002980CB File Offset: 0x002964CB
		public bool IsSlotLocked(Item.EquipmentSlot slot)
		{
			return this._equipmentSlotsLocked[(int)slot];
		}

		// Token: 0x0600562C RID: 22060 RVA: 0x002980D8 File Offset: 0x002964D8
		public bool Equip(int itemId, bool pickedUpFromWorld)
		{
			if (itemId == this.Logs._logItemId)
			{
				if (this.Logs.Lift())
				{
					EventRegistry.Player.Publish(TfEvent.EquippedItem, itemId);
					return true;
				}
				return false;
			}
			else
			{
				int num = this.AmountOf(itemId, false);
				if (pickedUpFromWorld)
				{
					num++;
					if (ItemDatabase.ItemById(itemId)._maxAmount > 0 && num > this.GetMaxAmountOf(itemId))
					{
						return false;
					}
				}
				if (num > 0 && this.Equip(this._inventoryItemViewsCache[itemId][Mathf.Min(num, this._inventoryItemViewsCache[itemId].Count) - 1], pickedUpFromWorld))
				{
					EventRegistry.Player.Publish(TfEvent.EquippedItem, itemId);
					return true;
				}
				return false;
			}
		}

		// Token: 0x0600562D RID: 22061 RVA: 0x002981A8 File Offset: 0x002965A8
		private bool Equip(InventoryItemView itemView, bool pickedUpFromWorld)
		{
			if (itemView != null)
			{
				this._equipPreviousTime = float.MaxValue;
				Item.EquipmentSlot equipmentSlot = itemView.ItemCache._equipmentSlot;
				if ((this.Logs.HasLogs && equipmentSlot != Item.EquipmentSlot.LeftHand) || (LocalPlayer.AnimControl.carry && equipmentSlot != Item.EquipmentSlot.LeftHand) || (LocalPlayer.Create.CreateMode && itemView.ItemCache.MatchType(Item.Types.Projectile | Item.Types.RangedWeapon | Item.Types.Weapon)) || (!(itemView != this._equipmentSlots[(int)equipmentSlot]) && !pickedUpFromWorld) || this.IsSlotLocked(equipmentSlot) || (itemView.ItemCache.MatchType(Item.Types.Special) && !this.SpecialItemsControlers[itemView._itemId].ToggleSpecial(true)))
				{
					return false;
				}
				if (pickedUpFromWorld || this.RemoveItem(itemView._itemId, 1, false, false))
				{
					this.LockEquipmentSlot(equipmentSlot);
					base.StartCoroutine(this.EquipSequence(equipmentSlot, itemView));
					return true;
				}
			}
			else
			{
				Debug.LogWarning(base.name + " is trying to equip a null object");
			}
			return false;
		}

		// Token: 0x0600562E RID: 22062 RVA: 0x002982D4 File Offset: 0x002966D4
		private IEnumerator EquipSequence(Item.EquipmentSlot slot, InventoryItemView itemView)
		{
			bool specialItemCheck = true;
			if (this._equipmentSlots[(int)slot] != null && this._equipmentSlots[(int)slot] != this._noEquipedItem)
			{
				this._pendingEquip = true;
				this._equipmentSlotsNext[(int)slot] = itemView;
				bool canStash = this._equipmentSlots[(int)slot].ItemCache._maxAmount >= 0;
				if (canStash)
				{
					this.MemorizeItem(slot);
				}
				this._itemAnimHash.ApplyAnimVars(this._equipmentSlots[(int)slot].ItemCache, false);
				int currentItemId = this._equipmentSlots[(int)slot]._itemId;
				if (Time.timeScale > 0f)
				{
					float durationCountdown = this._equipmentSlots[(int)slot].ItemCache._unequipDelay;
					while (this._pendingEquip && durationCountdown > 0f)
					{
						durationCountdown -= Time.deltaTime;
						yield return null;
					}
				}
				if (!this.HasInSlot(slot, currentItemId) || !this.HasInNextSlot(slot, itemView._itemId))
				{
					if (canStash)
					{
						this.AddItem(itemView._itemId, 1, true, true, null);
					}
					else
					{
						this.FakeDrop(itemView._itemId, null);
					}
					if (this._equipmentSlotsNext[(int)slot] == itemView)
					{
						this._equipmentSlotsNext[(int)slot] = this._noEquipedItem;
					}
					this._pendingEquip = false;
					yield break;
				}
				this.UnlockEquipmentSlot(slot);
				if (itemView.ItemCache.MatchType(Item.Types.Special))
				{
					specialItemCheck = this.SpecialItemsControlers[itemView._itemId].ToggleSpecial(true);
				}
				if (specialItemCheck)
				{
					this.UnequipItemAtSlot(slot, !canStash, canStash, false);
				}
				this._equipmentSlotsNext[(int)slot] = this._noEquipedItem;
			}
			else if (itemView.ItemCache.MatchType(Item.Types.Special))
			{
				specialItemCheck = this.SpecialItemsControlers[itemView._itemId].ToggleSpecial(true);
			}
			if (specialItemCheck)
			{
				if (itemView._held)
				{
					this._equipmentSlots[(int)slot] = itemView;
					itemView.OnItemEquipped();
					itemView._held.SetActive(true);
					HeldItemIdentifier heldItem = itemView._held.GetComponent<HeldItemIdentifier>();
					if (heldItem != null)
					{
						heldItem.Properties.Copy(itemView.Properties);
					}
					itemView.ApplyEquipmentEffect(true);
					this._itemAnimHash.ApplyAnimVars(itemView.ItemCache, true);
					if (itemView.ItemCache._equipedSFX != Item.SFXCommands.None)
					{
						LocalPlayer.Sfx.SendMessage(itemView.ItemCache._equipedSFX.ToString(), SendMessageOptions.DontRequireReceiver);
					}
					if (itemView.ItemCache._maxAmount >= 0)
					{
						this.ToggleAmmo(itemView, true);
						this.ToggleInventoryItemView(itemView._itemId, false, null);
					}
					yield return (!itemView.ItemCache.MatchType(Item.Types.Projectile)) ? null : YieldPresets.WaitPointSevenSeconds;
				}
				else
				{
					Debug.LogError("Trying to equip item '" + itemView.ItemCache._name + "' which doesn't have a held reference in " + itemView.name);
				}
				this.UnlockEquipmentSlot(slot);
			}
			this._pendingEquip = false;
			yield break;
		}

		// Token: 0x0600562F RID: 22063 RVA: 0x002982FD File Offset: 0x002966FD
		public void StopPendingEquip()
		{
			this._pendingEquip = false;
		}

		// Token: 0x06005630 RID: 22064 RVA: 0x00298306 File Offset: 0x00296706
		public void MemorizeItem(Item.EquipmentSlot slot)
		{
			if (Mathf.Approximately(this._equipPreviousTime, 3.4028235E+38f))
			{
				this._equipmentSlotsPrevious[(int)slot] = this._equipmentSlots[(int)slot];
			}
			else
			{
				this._equipPreviousTime = float.MaxValue;
			}
		}

		// Token: 0x06005631 RID: 22065 RVA: 0x0029833D File Offset: 0x0029673D
		public void MemorizeOverrideItem(Item.EquipmentSlot slot)
		{
			if (Mathf.Approximately(this._equipPreviousTime, 3.4028235E+38f))
			{
				this._equipmentSlotsPreviousOverride[(int)slot] = this._equipmentSlots[(int)slot];
			}
			else
			{
				this._equipPreviousTime = float.MaxValue;
			}
		}

		// Token: 0x06005632 RID: 22066 RVA: 0x00298374 File Offset: 0x00296774
		public void EquipPreviousUtility(bool keepPrevious = false)
		{
			if (this._equipmentSlotsPrevious[1] != null && this._equipmentSlotsPrevious[1] != this._noEquipedItem)
			{
				this.Equip(this._equipmentSlotsPrevious[1]._itemId, false);
				if (!keepPrevious)
				{
					this._equipmentSlotsPrevious[1] = null;
				}
			}
		}

		// Token: 0x06005633 RID: 22067 RVA: 0x002983D0 File Offset: 0x002967D0
		public void EquipPreviousWeaponDelayed()
		{
			this._equipPreviousTime = Time.time + 0.45f;
		}

		// Token: 0x06005634 RID: 22068 RVA: 0x002983E3 File Offset: 0x002967E3
		public void CancelEquipPreviousWeaponDelayed()
		{
			this._equipPreviousTime = float.MaxValue;
		}

		// Token: 0x06005635 RID: 22069 RVA: 0x002983F0 File Offset: 0x002967F0
		public void EquipPreviousWeapon(bool fallbackToDefault = true)
		{
			if (!this.EquipPreviousOverride() && this._equipmentSlotsPrevious[0] != this._noEquipedItem)
			{
				this._equipPreviousTime = float.MaxValue;
				if (this._equipmentSlotsPrevious[0] != null)
				{
					if (this.Equip(this._equipmentSlotsPrevious[0]._itemId, false))
					{
						this._equipmentSlotsPrevious[0] = null;
					}
					else
					{
						this.Equip(this._defaultWeaponItemId, false);
						this._equipmentSlotsPrevious[0] = this._inventoryItemViewsCache[this._defaultWeaponItemId][0];
					}
				}
				else if (fallbackToDefault)
				{
					this.Equip(this._defaultWeaponItemId, false);
					this._equipmentSlotsPrevious[0] = this._inventoryItemViewsCache[this._defaultWeaponItemId][0];
				}
			}
		}

		// Token: 0x06005636 RID: 22070 RVA: 0x002984CC File Offset: 0x002968CC
		private bool EquipPreviousOverride()
		{
			if (this._equipmentSlotsPreviousOverride[0] != this._noEquipedItem && this._equipmentSlotsPreviousOverride[0])
			{
				if (this.Equip(this._equipmentSlotsPreviousOverride[0]._itemId, false))
				{
					this._equipPreviousTime = float.MaxValue;
					this._equipmentSlotsPreviousOverride[0] = this._noEquipedItem;
					return true;
				}
				this._equipmentSlotsPreviousOverride[0] = this._noEquipedItem;
			}
			return false;
		}

		// Token: 0x06005637 RID: 22071 RVA: 0x00298546 File Offset: 0x00296946
		public void DropEquipedWeapon(bool equipPrevious)
		{
			Debug.Log("dropping right hand");
			this.DroppedRightHand.Invoke();
			LocalPlayer.Animator.SetBoolReflected("lookAtItemRight", false);
			this.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, true, false, equipPrevious);
		}

		// Token: 0x06005638 RID: 22072 RVA: 0x00298577 File Offset: 0x00296977
		public void StashEquipedWeapon(bool equipPrevious)
		{
			this.StashedRightHand.Invoke();
			this.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, false, true, equipPrevious);
		}

		// Token: 0x06005639 RID: 22073 RVA: 0x0029858E File Offset: 0x0029698E
		public void StashLeftHand()
		{
			this.StashedLeftHand.Invoke();
			if (this.HasInSlot(Item.EquipmentSlot.LeftHand, this.DefaultLight._itemId))
			{
				this.DefaultLight.StashLighter();
			}
			else
			{
				this.UnequipItemAtSlot(Item.EquipmentSlot.LeftHand, false, true, false);
			}
		}

		// Token: 0x0600563A RID: 22074 RVA: 0x002985CC File Offset: 0x002969CC
		public void HideAllEquiped(bool hideOnly = false, bool skipMemorize = false)
		{
			if (!hideOnly)
			{
				if (LocalPlayer.AnimControl.carry)
				{
					LocalPlayer.AnimControl.DropBody();
				}
				else if (this.Logs.HasLogs && this.Logs.PutDown(false, true, false, null))
				{
					this.Logs.PutDown(false, true, false, null);
				}
			}
			if (!this.IsSlotEmpty(Item.EquipmentSlot.LeftHand))
			{
				this.MemorizeItem(Item.EquipmentSlot.LeftHand);
			}
			if (!this.IsSlotEmpty(Item.EquipmentSlot.RightHand) && !skipMemorize)
			{
				this.MemorizeItem(Item.EquipmentSlot.RightHand);
			}
			this.StashLeftHand();
			this.StashEquipedWeapon(false);
		}

		// Token: 0x0600563B RID: 22075 RVA: 0x0029866B File Offset: 0x00296A6B
		public void ShowAllEquiped(bool fallbackToDefault = true)
		{
			this.EquipPreviousUtility(false);
			this.EquipPreviousWeapon(fallbackToDefault);
		}

		// Token: 0x0600563C RID: 22076 RVA: 0x0029867C File Offset: 0x00296A7C
		public void HideRightHand(bool hideOnly = false)
		{
			if (!hideOnly)
			{
				if (LocalPlayer.AnimControl.carry)
				{
					LocalPlayer.AnimControl.DropBody();
				}
				else if (this.Logs.HasLogs && this.Logs.PutDown(false, true, false, null))
				{
					this.Logs.PutDown(false, true, false, null);
				}
			}
			this.MemorizeItem(Item.EquipmentSlot.RightHand);
			this.StashEquipedWeapon(false);
		}

		// Token: 0x0600563D RID: 22077 RVA: 0x002986F0 File Offset: 0x00296AF0
		public void ShowRightHand(bool fallbackToDefault = true)
		{
			this.EquipPreviousWeapon(fallbackToDefault);
		}

		// Token: 0x0600563E RID: 22078 RVA: 0x002986FC File Offset: 0x00296AFC
		public void Attack()
		{
			if (!this.IsRightHandEmpty() && !this._isThrowing && !this.IsReloading && !this.blockRangedAttack && !this.IsSlotLocked(Item.EquipmentSlot.RightHand) && !LocalPlayer.Inventory.HasInSlot(Item.EquipmentSlot.RightHand, LocalPlayer.AnimControl._slingShotId))
			{
				TheForest.Utils.Input.ResetDelayedAction();
				Item itemCache = this._equipmentSlots[0].ItemCache;
				if (itemCache.MatchType(Item.Types.RangedWeapon) && itemCache.HasLastAmmoAttackEvent() && this.AmountOf(itemCache._ammoItemId, false) == 1)
				{
					LocalPlayer.Stats.UsedStick();
					if (itemCache.CanDoLastAmmoAttackEvent())
					{
						this._pm.SendEvent(itemCache._lastAmmoAttackEvent.ToString());
					}
				}
				else if (itemCache.HasAttackEvent())
				{
					LocalPlayer.Stats.UsedStick();
					if (itemCache.CanDoAttackEvent())
					{
						this._pm.SendEvent(itemCache._attackEvent.ToString());
					}
				}
				if (itemCache._attackSFX != Item.SFXCommands.None)
				{
					LocalPlayer.Sfx.SendMessage(itemCache._attackSFX.ToString(), SendMessageOptions.DontRequireReceiver);
				}
				if (itemCache.MatchType(Item.Types.Projectile))
				{
					if (!ForestVR.Enabled)
					{
						this._isThrowing = true;
						base.Invoke("ThrowProjectile", itemCache._projectileThrowDelay);
						LocalPlayer.TargetFunctions.Invoke("sendPlayerAttacking", 0.5f);
						LocalPlayer.SpecialItems.SendMessage("stopLightHeldFire", SendMessageOptions.DontRequireReceiver);
					}
				}
				else if (itemCache.MatchType(Item.Types.RangedWeapon))
				{
					if (itemCache.MatchRangedStyle(Item.RangedStyle.Instantaneous))
					{
						base.Invoke("FireRangedWeapon", itemCache._projectileThrowDelay);
					}
					else
					{
						this._weaponChargeStartTime = Time.time;
					}
				}
				this.Attacked.Invoke();
			}
		}

		// Token: 0x0600563F RID: 22079 RVA: 0x002988D8 File Offset: 0x00296CD8
		public void ReleaseAttack()
		{
			if (!this.IsRightHandEmpty() && !this._isThrowing && !this.blockRangedAttack && !LocalPlayer.Inventory.HasInSlot(Item.EquipmentSlot.RightHand, LocalPlayer.AnimControl._slingShotId))
			{
				Item itemCache = this._equipmentSlots[0].ItemCache;
				if (this.CancelNextChargedAttack)
				{
					this.CancelNextChargedAttack = false;
					return;
				}
				if (itemCache.MatchType(Item.Types.RangedWeapon) && itemCache.MatchRangedStyle(Item.RangedStyle.Charged))
				{
					if (itemCache.HasAttackReleaseEvent())
					{
						LocalPlayer.Stats.UsedStick();
						if (itemCache.CanDoAttackReleaseEvent())
						{
							this._pm.SendEvent(itemCache._attackReleaseEvent.ToString());
						}
					}
					this._isThrowing = true;
					base.Invoke("FireRangedWeapon", itemCache._projectileThrowDelay);
					this.ReleasedAttack.Invoke();
				}
			}
		}

		// Token: 0x06005640 RID: 22080 RVA: 0x002989BC File Offset: 0x00296DBC
		public void Block()
		{
			this.Blocking(true);
			if (!this.IsRightHandEmpty() && !this._isThrowing)
			{
				Item itemCache = this._equipmentSlots[0].ItemCache;
				if (itemCache.HasBlockEvent())
				{
					LocalPlayer.Stats.UsedStick();
					if (itemCache.CanDoBlockEvent())
					{
						this._pm.SendEvent(itemCache._blockEvent.ToString());
					}
					this.Blocked.Invoke();
				}
			}
		}

		// Token: 0x06005641 RID: 22081 RVA: 0x00298A3C File Offset: 0x00296E3C
		public void UnBlock()
		{
			this.Blocking(false);
			if (!this.IsRightHandEmpty())
			{
				Item itemCache = this._equipmentSlots[0].ItemCache;
				if (itemCache.HasUnblockEvent())
				{
					LocalPlayer.Stats.UsedStick();
					if (itemCache.CanDoUnblockEvent())
					{
						this._pm.SendEvent(itemCache._unblockEvent.ToString());
					}
					this.Unblocked.Invoke();
				}
			}
		}

		// Token: 0x06005642 RID: 22082 RVA: 0x00298AB0 File Offset: 0x00296EB0
		private void Blocking(bool onoff)
		{
			if (TheForest.Utils.Input.IsGamePad)
			{
				this.BlockTogglingInventory = onoff;
				this.QuickSelectGamepadSwitch = onoff;
			}
			else if (this.QuickSelectGamepadSwitch)
			{
				this.BlockTogglingInventory = false;
				this.QuickSelectGamepadSwitch = false;
			}
		}

		// Token: 0x06005643 RID: 22083 RVA: 0x00298AE8 File Offset: 0x00296EE8
		public bool AddItem(int itemId, int amount = 1, bool preventAutoEquip = false, bool fromCraftingCog = false, ItemProperties properties = null)
		{
			if (this.ItemFilter != null)
			{
				return this.ItemFilter.AddItem(itemId, amount, preventAutoEquip, fromCraftingCog, properties);
			}
			return this.AddItemNF(itemId, amount, preventAutoEquip, fromCraftingCog, properties);
		}

		// Token: 0x06005644 RID: 22084 RVA: 0x00298B18 File Offset: 0x00296F18
		public bool AddItemNF(int itemId, int amount = 1, bool preventAutoEquip = false, bool fromCraftingCog = false, ItemProperties properties = null)
		{
			if (this.Logs != null && itemId == this.Logs._logItemId)
			{
				return this.Logs.Lift();
			}
			Item item = ItemDatabase.ItemById(itemId);
			if (item == null)
			{
				return false;
			}
			if (item._maxAmount >= 0)
			{
				if (amount < 1)
				{
					return true;
				}
				if (!Application.isPlaying || fromCraftingCog || Mathf.Approximately(item._weight, 0f) || item._weight + LocalPlayer.Stats.CarriedWeight.CurrentWeight <= 1f)
				{
					InventoryItem inventoryItem;
					if (!this._possessedItemCache.ContainsKey(itemId))
					{
						inventoryItem = new InventoryItem
						{
							_itemId = itemId,
							_amount = 0,
							_maxAmount = ((item._maxAmount != 0) ? item._maxAmount : int.MaxValue)
						};
						this._possessedItems.Add(inventoryItem);
						this._possessedItemCache[itemId] = inventoryItem;
					}
					else
					{
						inventoryItem = this._possessedItemCache[itemId];
					}
					if (Application.isPlaying)
					{
						int num = inventoryItem.Add(amount, this.HasInSlot(item._equipmentSlot, itemId));
						if (num < amount)
						{
							if (!fromCraftingCog)
							{
								this.RefreshCurrentWeight();
								Scene.HudGui.ToggleGotItemHud(itemId, amount - num);
							}
							ItemUtils.ApplyEffectsToStats(item._ownedStatEffect, true, amount - num);
						}
						if (num > 0)
						{
							Scene.HudGui.ToggleFullCapacityHud(itemId);
							if (num == amount && !fromCraftingCog)
							{
								return false;
							}
						}
						if (item.MatchType(Item.Types.Special) && this.SpecialItemsControlers.ContainsKey(itemId))
						{
							this.SpecialItemsControlers[itemId].PickedUpSpecialItem(itemId);
						}
						if (preventAutoEquip || LocalPlayer.AnimControl.swimming || !item.MatchType(Item.Types.Equipment) || (!(this._equipmentSlots[(int)item._equipmentSlot] == null) && !(this._equipmentSlots[0] == this._noEquipedItem)) || !this.Equip(itemId, false))
						{
							EventRegistry.Player.Publish(TfEvent.AddedItem, itemId);
						}
						this.ToggleInventoryItemView(itemId, false, properties);
						if (this.SkipNextAddItemWoosh)
						{
							this.SkipNextAddItemWoosh = false;
						}
						else
						{
							LocalPlayer.Sfx.PlayItemCustomSfx(item, (!Grabber.FocusedItem) ? (LocalPlayer.Transform.position + LocalPlayer.MainCamTr.forward) : Grabber.FocusedItem.transform.position, true);
						}
					}
					else
					{
						inventoryItem.Add(amount, false);
					}
					return true;
				}
				Scene.HudGui.ToggleFullWeightHud(itemId);
				return false;
			}
			else
			{
				if (!item.MatchType(Item.Types.Equipment) || LocalPlayer.AnimControl.swimming)
				{
					return false;
				}
				bool flag = this._equipmentSlots[(int)item._equipmentSlot] == null || this._equipmentSlots[0] == this._noEquipedItem || this._equipmentSlots[(int)item._equipmentSlot]._itemId != itemId;
				if (flag && !this.Logs.HasLogs && this.Equip(itemId, true))
				{
					EventRegistry.Player.Publish(TfEvent.AddedItem, itemId);
					return true;
				}
				return false;
			}
		}

		// Token: 0x06005645 RID: 22085 RVA: 0x00298E76 File Offset: 0x00297276
		public void RefreshCurrentWeight()
		{
		}

		// Token: 0x06005646 RID: 22086 RVA: 0x00298E78 File Offset: 0x00297278
		public bool RemoveItem(int itemId, int amount = 1, bool allowAmountOverflow = false, bool shouldEquipPrevious = true)
		{
			if (this.ItemFilter != null)
			{
				return this.ItemFilter.RemoveItem(itemId, amount, allowAmountOverflow);
			}
			return this.RemoveItemNF(itemId, amount, allowAmountOverflow, shouldEquipPrevious);
		}

		// Token: 0x06005647 RID: 22087 RVA: 0x00298EA0 File Offset: 0x002972A0
		public bool RemoveItemNF(int itemId, int amount = 1, bool allowAmountOverflow = false, bool shouldEquipPrevious = true)
		{
			InventoryItem inventoryItem;
			if (this.Logs != null && itemId == this.Logs._logItemId)
			{
				if (this.Logs.PutDown(false, false, true, null))
				{
					EventRegistry.Player.Publish(TfEvent.RemovedItem, itemId);
					return true;
				}
			}
			else if (this._possessedItemCache.TryGetValue(itemId, out inventoryItem) && inventoryItem._amount > 0)
			{
				if (inventoryItem.Remove(amount))
				{
					if (Application.isPlaying)
					{
						this.RefreshCurrentWeight();
						this.ToggleInventoryItemView(itemId, false, null);
						ItemUtils.ApplyEffectsToStats(ItemDatabase.ItemById(itemId)._ownedStatEffect, false, amount);
					}
					EventRegistry.Player.Publish(TfEvent.RemovedItem, itemId);
					return true;
				}
				if (allowAmountOverflow)
				{
					if (this.HasInSlot(Item.EquipmentSlot.RightHand, itemId))
					{
						ItemUtils.ApplyEffectsToStats(ItemDatabase.ItemById(itemId)._ownedStatEffect, false, 1);
						this.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, false, false, shouldEquipPrevious);
						EventRegistry.Player.Publish(TfEvent.RemovedItem, itemId);
					}
					if (inventoryItem._maxAmountBonus == 0)
					{
						this._possessedItems.Remove(inventoryItem);
						this._possessedItemCache.Remove(itemId);
					}
					this.ToggleInventoryItemView(itemId, false, null);
					this.RefreshCurrentWeight();
				}
			}
			else
			{
				if (amount == 1 && this.HasInSlot(Item.EquipmentSlot.RightHand, itemId))
				{
					this.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, false, false, shouldEquipPrevious);
					EventRegistry.Player.Publish(TfEvent.RemovedItem, itemId);
					this.RefreshCurrentWeight();
					return true;
				}
				if (amount == 1 && this.HasInSlot(Item.EquipmentSlot.LeftHand, itemId))
				{
					this.UnequipItemAtSlot(Item.EquipmentSlot.LeftHand, false, false, shouldEquipPrevious);
					EventRegistry.Player.Publish(TfEvent.RemovedItem, itemId);
					this.RefreshCurrentWeight();
					return true;
				}
				Item item = ItemDatabase.ItemById(itemId);
				if (item._fallbackItemIds.Length > 0)
				{
					for (int i = 0; i < item._fallbackItemIds.Length; i++)
					{
						if (this.RemoveItem(item._fallbackItemIds[i], amount, allowAmountOverflow, true))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x06005648 RID: 22088 RVA: 0x002990AB File Offset: 0x002974AB
		public bool HasOwned(int itemId)
		{
			return (this.Logs != null && itemId == this.Logs._logItemId) || this._possessedItemCache.ContainsKey(itemId);
		}

		// Token: 0x06005649 RID: 22089 RVA: 0x002990DD File Offset: 0x002974DD
		public bool IsSlotEmpty(Item.EquipmentSlot slot)
		{
			return this._equipmentSlots[(int)slot] == null || this._equipmentSlots[(int)slot] == this._noEquipedItem;
		}

		// Token: 0x0600564A RID: 22090 RVA: 0x00299108 File Offset: 0x00297508
		public bool IsSlotNextEmpty(Item.EquipmentSlot slot)
		{
			return this._equipmentSlotsNext[(int)slot] == null || this._equipmentSlotsNext[(int)slot] == this._noEquipedItem;
		}

		// Token: 0x0600564B RID: 22091 RVA: 0x00299133 File Offset: 0x00297533
		public bool IsSlotAndNextSlotEmpty(Item.EquipmentSlot slot)
		{
			return this.IsSlotEmpty(slot) && this.IsSlotNextEmpty(slot);
		}

		// Token: 0x0600564C RID: 22092 RVA: 0x0029914B File Offset: 0x0029754B
		public bool IsRightHandEmpty()
		{
			return this.IsSlotEmpty(Item.EquipmentSlot.RightHand);
		}

		// Token: 0x0600564D RID: 22093 RVA: 0x00299154 File Offset: 0x00297554
		public bool IsLeftHandEmpty()
		{
			return this.IsSlotEmpty(Item.EquipmentSlot.LeftHand);
		}

		// Token: 0x0600564E RID: 22094 RVA: 0x0029915D File Offset: 0x0029755D
		public bool HasInSlot(Item.EquipmentSlot slot, int itemId)
		{
			return this._equipmentSlots[(int)slot] != null && this._equipmentSlots[(int)slot]._itemId == itemId;
		}

		// Token: 0x0600564F RID: 22095 RVA: 0x00299185 File Offset: 0x00297585
		public bool HasInPreviousSlot(Item.EquipmentSlot slot, int itemId)
		{
			return this._equipmentSlotsPrevious[(int)slot] != null && this._equipmentSlotsPrevious[(int)slot]._itemId == itemId;
		}

		// Token: 0x06005650 RID: 22096 RVA: 0x002991AD File Offset: 0x002975AD
		public bool HasInNextSlot(Item.EquipmentSlot slot, int itemId)
		{
			return this._equipmentSlotsNext[(int)slot] != null && this._equipmentSlotsNext[(int)slot]._itemId == itemId;
		}

		// Token: 0x06005651 RID: 22097 RVA: 0x002991D8 File Offset: 0x002975D8
		public bool HasInSlotOrNextSlot(Item.EquipmentSlot slot, int itemId)
		{
			return (this._equipmentSlots[(int)slot] != null && this._equipmentSlots[(int)slot]._itemId == itemId) || (this._equipmentSlotsNext[(int)slot] != null && this._equipmentSlotsNext[(int)slot]._itemId == itemId);
		}

		// Token: 0x06005652 RID: 22098 RVA: 0x00299234 File Offset: 0x00297634
		public bool Owns(int itemId, bool allowFallback = true)
		{
			if (this.ItemFilter != null)
			{
				return this.ItemFilter.Owns(itemId, allowFallback);
			}
			return this.OwnsNF(itemId, allowFallback);
		}

		// Token: 0x06005653 RID: 22099 RVA: 0x00299258 File Offset: 0x00297658
		public bool OwnsNF(int itemId, bool allowFallback = true)
		{
			if (this.Logs != null && itemId == this.Logs._logItemId)
			{
				return this.Logs.HasLogs;
			}
			bool flag = (this._possessedItemCache.ContainsKey(itemId) && this._possessedItemCache[itemId]._amount > 0) || this.HasInSlotOrNextSlot(Item.EquipmentSlot.RightHand, itemId) || this.HasInSlotOrNextSlot(Item.EquipmentSlot.LeftHand, itemId) || this.HasInSlotOrNextSlot(Item.EquipmentSlot.Chest, itemId) || this.HasInSlotOrNextSlot(Item.EquipmentSlot.Feet, itemId) || this.HasInSlotOrNextSlot(Item.EquipmentSlot.FullBody, itemId);
			if (!flag && allowFallback)
			{
				Item item = ItemDatabase.ItemById(itemId);
				if (item._fallbackItemIds.Length > 0)
				{
					for (int i = 0; i < item._fallbackItemIds.Length; i++)
					{
						if (this.Owns(item._fallbackItemIds[i], true))
						{
							return true;
						}
					}
				}
			}
			return flag;
		}

		// Token: 0x06005654 RID: 22100 RVA: 0x0029934C File Offset: 0x0029774C
		public int OwnsWhich(int itemId, bool allowFallback = true)
		{
			if (this.Logs != null && itemId == this.Logs._logItemId)
			{
				return itemId;
			}
			bool flag = (this._possessedItemCache.ContainsKey(itemId) && this._possessedItemCache[itemId]._amount > 0) || this.HasInSlot(Item.EquipmentSlot.RightHand, itemId) || this.HasInSlot(Item.EquipmentSlot.LeftHand, itemId) || this.HasInSlot(Item.EquipmentSlot.Chest, itemId) || this.HasInSlot(Item.EquipmentSlot.Feet, itemId) || this.HasInSlot(Item.EquipmentSlot.FullBody, itemId);
			if (!flag && allowFallback)
			{
				Item item = ItemDatabase.ItemById(itemId);
				if (item._fallbackItemIds.Length > 0)
				{
					for (int i = 0; i < item._fallbackItemIds.Length; i++)
					{
						int num = this.OwnsWhich(item._fallbackItemIds[i], true);
						if (num > -1)
						{
							return num;
						}
					}
				}
			}
			return (!flag) ? (-1) : itemId;
		}

		// Token: 0x06005655 RID: 22101 RVA: 0x00299445 File Offset: 0x00297845
		public int AmountOf(int itemId, bool allowFallback = true)
		{
			if (this.ItemFilter != null)
			{
				return this.ItemFilter.AmountOf(itemId, allowFallback);
			}
			return this.AmountOfNF(itemId, allowFallback);
		}

		// Token: 0x06005656 RID: 22102 RVA: 0x00299468 File Offset: 0x00297868
		public int AmountOfNF(int itemId, bool allowFallback = true)
		{
			if (this.Logs != null && itemId == this.Logs._logItemId)
			{
				return this.Logs.Amount;
			}
			int num = 0;
			if (this._possessedItemCache.ContainsKey(itemId))
			{
				num = this._possessedItemCache[itemId]._amount;
			}
			if (this.HasInSlot(Item.EquipmentSlot.RightHand, itemId) || this.HasInSlot(Item.EquipmentSlot.LeftHand, itemId) || this.HasInSlot(Item.EquipmentSlot.Chest, itemId) || this.HasInSlot(Item.EquipmentSlot.Feet, itemId) || this.HasInSlot(Item.EquipmentSlot.FullBody, itemId))
			{
				num++;
			}
			if (num == 0 && allowFallback)
			{
				Item item = ItemDatabase.ItemById(itemId);
				if (item._fallbackItemIds.Length > 0)
				{
					for (int i = 0; i < item._fallbackItemIds.Length; i++)
					{
						num += this.AmountOf(item._fallbackItemIds[i], false);
					}
				}
			}
			return num;
		}

		// Token: 0x06005657 RID: 22103 RVA: 0x0029955C File Offset: 0x0029795C
		public bool HasRoomFor(int itemId, int amount = 1)
		{
			int num = this.AmountOf(itemId, false);
			int maxAmountOf = this.GetMaxAmountOf(itemId);
			return num + amount <= maxAmountOf;
		}

		// Token: 0x06005658 RID: 22104 RVA: 0x00299584 File Offset: 0x00297984
		public void AddMaxAmountBonus(int itemId, int amount)
		{
			if (this.Logs != null && itemId != this.Logs._logItemId)
			{
				InventoryItem inventoryItem;
				if (!this._possessedItemCache.ContainsKey(itemId))
				{
					Item item = ItemDatabase.ItemById(itemId);
					inventoryItem = new InventoryItem
					{
						_itemId = itemId,
						_amount = 0,
						_maxAmount = ((item._maxAmount != 0) ? item._maxAmount : int.MaxValue)
					};
					this._possessedItems.Add(inventoryItem);
					this._possessedItemCache[itemId] = inventoryItem;
				}
				else
				{
					inventoryItem = this._possessedItemCache[itemId];
				}
				inventoryItem._maxAmountBonus += amount;
			}
		}

		// Token: 0x06005659 RID: 22105 RVA: 0x0029963C File Offset: 0x00297A3C
		public void SetMaxAmountBonus(int itemId, int amount)
		{
			if (this.Logs != null && itemId != this.Logs._logItemId)
			{
				InventoryItem inventoryItem;
				if (!this._possessedItemCache.ContainsKey(itemId))
				{
					Item item = ItemDatabase.ItemById(itemId);
					inventoryItem = new InventoryItem
					{
						_itemId = itemId,
						_amount = 0,
						_maxAmount = ((item._maxAmount != 0) ? item._maxAmount : int.MaxValue)
					};
					this._possessedItems.Add(inventoryItem);
					this._possessedItemCache[itemId] = inventoryItem;
				}
				else
				{
					inventoryItem = this._possessedItemCache[itemId];
				}
				inventoryItem._maxAmountBonus = amount;
			}
		}

		// Token: 0x0600565A RID: 22106 RVA: 0x002996EC File Offset: 0x00297AEC
		public int GetMaxAmountOf(int itemId)
		{
			if (this._possessedItemCache.ContainsKey(itemId))
			{
				return this._possessedItemCache[itemId].MaxAmount;
			}
			int maxAmount = ItemDatabase.ItemById(itemId)._maxAmount;
			return (maxAmount != 0) ? maxAmount : int.MaxValue;
		}

		// Token: 0x0600565B RID: 22107 RVA: 0x0029973B File Offset: 0x00297B3B
		public void TurnOffLastLight()
		{
			if (this.HasInSlot(Item.EquipmentSlot.LeftHand, this.LastLight._itemId))
			{
				this.StashLeftHand();
			}
		}

		// Token: 0x0600565C RID: 22108 RVA: 0x0029975C File Offset: 0x00297B5C
		public void TurnOnLastLight()
		{
			if (!this.HasInSlot(Item.EquipmentSlot.LeftHand, this.LastLight._itemId))
			{
				if (!this.Equip(this.LastLight._itemId, false))
				{
					this.LastLight = this.DefaultLight;
					this.Equip(this.LastLight._itemId, false);
				}
				LocalPlayer.Tuts.HideLighter();
			}
		}

		// Token: 0x0600565D RID: 22109 RVA: 0x002997C0 File Offset: 0x00297BC0
		public void TurnOffLastUtility(Item.EquipmentSlot slot = Item.EquipmentSlot.LeftHand)
		{
			if (this.HasInSlot(slot, this.LastUtility._itemId))
			{
				this.StashLeftHand();
			}
		}

		// Token: 0x0600565E RID: 22110 RVA: 0x002997DF File Offset: 0x00297BDF
		public void TurnOnLastUtility(Item.EquipmentSlot slot = Item.EquipmentSlot.LeftHand)
		{
			if (!this.HasInSlot(slot, this.LastUtility._itemId))
			{
				this.Equip(this.LastUtility._itemId, false);
			}
		}

		// Token: 0x0600565F RID: 22111 RVA: 0x0029980C File Offset: 0x00297C0C
		public void BloodyWeapon()
		{
			foreach (InventoryItemView inventoryItemView in this._equipmentSlots)
			{
				if (inventoryItemView != null && inventoryItemView._held != null)
				{
					inventoryItemView._held.SendMessage("GotBloody", SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		// Token: 0x06005660 RID: 22112 RVA: 0x00299868 File Offset: 0x00297C68
		public void CleanWeapon()
		{
			foreach (InventoryItemView inventoryItemView in this._equipmentSlots)
			{
				if (inventoryItemView != null && inventoryItemView._held != null)
				{
					inventoryItemView._held.SendMessage("GotClean", SendMessageOptions.DontRequireReceiver);
				}
			}
			foreach (Bloodify bloodify in LocalPlayer.HeldItemsData._weaponBlood)
			{
				if (bloodify)
				{
					bloodify.GotClean();
				}
			}
			foreach (BurnableCloth burnableCloth in LocalPlayer.HeldItemsData._weaponFire)
			{
				if (burnableCloth)
				{
					burnableCloth.GotClean();
				}
			}
			foreach (InventoryItemView inventoryItemView2 in this._equipmentSlots)
			{
				if (inventoryItemView2 != null && inventoryItemView2._held != null)
				{
					inventoryItemView2._held.SendMessage("GotClean", SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		// Token: 0x06005661 RID: 22113 RVA: 0x00299996 File Offset: 0x00297D96
		public void GotLeaf()
		{
			this.AddItem(this._leafItemId, global::UnityEngine.Random.Range(1, 3), false, false, null);
		}

		// Token: 0x06005662 RID: 22114 RVA: 0x002999B0 File Offset: 0x00297DB0
		public void GotSap(int? amount = null)
		{
			int num = ((amount == null) ? global::UnityEngine.Random.Range(-1, 3) : amount.Value);
			if (num > 0)
			{
				this.AddItem(this._sapItemId, num, false, false, null);
			}
		}

		// Token: 0x06005663 RID: 22115 RVA: 0x002999F5 File Offset: 0x00297DF5
		public void HighlightItemGroup(InventoryItemView view, bool onoff)
		{
			this.HighlightItemGroup(view._itemId, view.Properties, onoff);
		}

		// Token: 0x06005664 RID: 22116 RVA: 0x00299A0C File Offset: 0x00297E0C
		public void HighlightItemGroup(int itemId, ItemProperties properties, bool onoff)
		{
			if (this._inventoryItemViewsCache.ContainsKey(itemId))
			{
				List<InventoryItemView> list = this._inventoryItemViewsCache[itemId];
				int count = list.Count;
				for (int i = count - 1; i >= 0; i--)
				{
					InventoryItemView inventoryItemView = list[i];
					if (inventoryItemView && inventoryItemView.gameObject.activeSelf)
					{
						if (onoff)
						{
							if (inventoryItemView.Properties.Match(properties))
							{
								inventoryItemView.Highlight(true);
							}
						}
						else
						{
							inventoryItemView.Highlight(false);
						}
					}
				}
			}
		}

		// Token: 0x06005665 RID: 22117 RVA: 0x00299AA0 File Offset: 0x00297EA0
		public void SheenItem(int itemId, ItemProperties properties, bool onoff)
		{
			if (this._inventoryItemViewsCache.ContainsKey(itemId))
			{
				List<InventoryItemView> list = this._inventoryItemViewsCache[itemId];
				int count = list.Count;
				for (int i = count - 1; i >= 0; i--)
				{
					InventoryItemView inventoryItemView = list[i];
					if (inventoryItemView && inventoryItemView.Properties.Match(properties))
					{
						inventoryItemView.Sheen(onoff);
					}
				}
			}
		}

		// Token: 0x06005666 RID: 22118 RVA: 0x00299B14 File Offset: 0x00297F14
		public InventoryItemView GetLastActiveView(int itemId)
		{
			int num = this.AmountOf(itemId, false);
			if (num > 0)
			{
				return this._inventoryItemViewsCache[itemId][Mathf.Min(num, this._inventoryItemViewsCache[itemId].Count) - 1];
			}
			return null;
		}

		// Token: 0x06005667 RID: 22119 RVA: 0x00299B60 File Offset: 0x00297F60
		public void BubbleUpInventoryView(InventoryItemView view)
		{
			int num = Mathf.Max(this.AmountOf(view._itemId, false), 1);
			List<InventoryItemView> list = this._inventoryItemViewsCache[view._itemId];
			if (num <= list.Count)
			{
				list.Remove(view);
				list.Insert(num - 1, view);
			}
		}

		// Token: 0x06005668 RID: 22120 RVA: 0x00299BB4 File Offset: 0x00297FB4
		public void BubbleDownInventoryView(InventoryItemView view)
		{
			List<InventoryItemView> list = this._inventoryItemViewsCache[view._itemId];
			list.Remove(view);
			list.Insert(0, view);
		}

		// Token: 0x06005669 RID: 22121 RVA: 0x00299BE4 File Offset: 0x00297FE4
		public void ShuffleInventoryView(InventoryItemView view)
		{
			List<InventoryItemView> list = this._inventoryItemViewsCache[view._itemId];
			int num = Mathf.Min(Mathf.Max(this.AmountOf(view._itemId, false), 1), list.Count) - 1;
			int num2 = list.IndexOf(view);
			if (num2 == num)
			{
				list.Remove(view);
				if (num > 0)
				{
					list.Insert(0, view);
				}
				else
				{
					list.Insert(list.Count, view);
				}
				this.ToggleInventoryItemView(view._itemId, false, null);
			}
		}

		// Token: 0x0600566A RID: 22122 RVA: 0x00299C6C File Offset: 0x0029806C
		public bool ShuffleRemoveRightHandItem()
		{
			if (!this.IsRightHandEmpty())
			{
				if (LocalPlayer.Inventory.AmountOf(this.RightHand._itemId, false) == 1)
				{
					LocalPlayer.Inventory.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, false, false, true);
				}
				else
				{
					LocalPlayer.Inventory.MemorizeItem(Item.EquipmentSlot.RightHand);
					LocalPlayer.Inventory.ShuffleInventoryView(LocalPlayer.Inventory.RightHand);
					LocalPlayer.Inventory.UnequipItemAtSlot(Item.EquipmentSlot.RightHand, false, false, true);
				}
				return true;
			}
			return false;
		}

		// Token: 0x0600566B RID: 22123 RVA: 0x00299CE4 File Offset: 0x002980E4
		public void SortInventoryViewsByBonus(InventoryItemView view, WeaponStatUpgrade.Types activeBonus, bool setTargetViewFirst)
		{
			List<InventoryItemView> list = this._inventoryItemViewsCache[view._itemId];
			int num = Mathf.Max(this.AmountOf(view._itemId, false), 1);
			int num2 = 0;
			int count = list.Count;
			for (int i = count - 1; i >= 0; i--)
			{
				InventoryItemView inventoryItemView = list[i + num2];
				if (inventoryItemView && inventoryItemView.ActiveBonus != activeBonus && inventoryItemView.gameObject.activeSelf)
				{
					list.RemoveAt(i + num2++);
					list.Insert(0, inventoryItemView);
				}
			}
			if (setTargetViewFirst && num <= list.Count)
			{
				list.Remove(view);
				list.Insert(num - 1, view);
			}
		}

		// Token: 0x0600566C RID: 22124 RVA: 0x00299DAC File Offset: 0x002981AC
		public bool OwnsItemWithBonus(int itemId, WeaponStatUpgrade.Types bonus)
		{
			List<InventoryItemView> list = this._inventoryItemViewsCache[itemId];
			for (int i = list.Count - 1; i >= 0; i--)
			{
				InventoryItemView inventoryItemView = list[i];
				if (inventoryItemView && inventoryItemView.gameObject.activeSelf && inventoryItemView.ActiveBonus == bonus)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600566D RID: 22125 RVA: 0x00299E14 File Offset: 0x00298214
		public int AmountOfItemWithBonus(int itemId, WeaponStatUpgrade.Types bonus)
		{
			int num = 0;
			List<InventoryItemView> list = this._inventoryItemViewsCache[itemId];
			if (list[0].ItemCache._maxAmount > 0 && list[0].ItemCache._maxAmount <= list.Count)
			{
				for (int i = list.Count - 1; i >= 0; i--)
				{
					InventoryItemView inventoryItemView = list[i];
					if (inventoryItemView && inventoryItemView.gameObject.activeSelf && inventoryItemView.ActiveBonus == bonus)
					{
						num++;
					}
				}
			}
			else
			{
				num = this.AmountOf(itemId, false);
			}
			return num;
		}

		// Token: 0x0600566E RID: 22126 RVA: 0x00299EC0 File Offset: 0x002982C0
		public void AddUpgradeToCounter(int itemId, int upgradeItemId, int amount)
		{
			if (!this.ItemsUpgradeCounters.ContainsKey(itemId))
			{
				this.ItemsUpgradeCounters[itemId] = new PlayerInventory.UpgradeCounterDict();
			}
			int num;
			this.ItemsUpgradeCounters[itemId].TryGetValue(upgradeItemId, out num);
			this.ItemsUpgradeCounters[itemId][upgradeItemId] = num + amount;
		}

		// Token: 0x0600566F RID: 22127 RVA: 0x00299F19 File Offset: 0x00298319
		public int GetAmountOfUpgrades(int itemId)
		{
			if (this.ItemsUpgradeCounters.ContainsKey(itemId))
			{
				return this.ItemsUpgradeCounters[itemId].Values.Sum();
			}
			return 0;
		}

		// Token: 0x06005670 RID: 22128 RVA: 0x00299F44 File Offset: 0x00298344
		public int GetAmountOfUpgrades(int itemId, int upgradeItemId)
		{
			if (this.ItemsUpgradeCounters.ContainsKey(itemId) && this.ItemsUpgradeCounters[itemId].ContainsKey(upgradeItemId))
			{
				return this.ItemsUpgradeCounters[itemId][upgradeItemId];
			}
			return 0;
		}

		// Token: 0x06005671 RID: 22129 RVA: 0x00299F84 File Offset: 0x00298384
		public void GatherWater(bool clean)
		{
			InventoryItemView inventoryItemView = this._equipmentSlots[0];
			inventoryItemView.ActiveBonus = ((!clean) ? WeaponStatUpgrade.Types.DirtyWater : WeaponStatUpgrade.Types.CleanWater);
			inventoryItemView.Properties.ActiveBonusValue = 2f;
		}

		// Token: 0x06005672 RID: 22130 RVA: 0x00299FC0 File Offset: 0x002983C0
		public void SetQuickSelectItemIds(int[] itemIds)
		{
			this._quickSelectItemIds = itemIds;
			foreach (QuickSelectViews quickSelectViews2 in LocalPlayer.QuickSelectViews)
			{
				quickSelectViews2.ShowLocalPlayerViews();
			}
		}

		// Token: 0x06005673 RID: 22131 RVA: 0x00299FF8 File Offset: 0x002983F8
		private IEnumerator OnSerializing()
		{
			this._possessedItemsCount = this._possessedItems.Count;
			PlayerInventory.SerializableItemUpgradeCounters[] upgradeCounters;
			this._upgradeCounters = this.ItemsUpgradeCounters.Select(delegate(KeyValuePair<int, PlayerInventory.UpgradeCounterDict> itemUpgradeCounters)
			{
				PlayerInventory.SerializableItemUpgradeCounters serializableItemUpgradeCounters2 = new PlayerInventory.SerializableItemUpgradeCounters();
				serializableItemUpgradeCounters2._itemId = itemUpgradeCounters.Key;
				serializableItemUpgradeCounters2._counters = itemUpgradeCounters.Value.Select((KeyValuePair<int, int> upgradeCounters) => new PlayerInventory.SerializableUpgradeCounter
				{
					_upgradeItemId = upgradeCounters.Key,
					_amount = upgradeCounters.Value
				}).ToArray<PlayerInventory.SerializableUpgradeCounter>();
				return serializableItemUpgradeCounters2;
			}).ToArray<PlayerInventory.SerializableItemUpgradeCounters>();
			foreach (PlayerInventory.SerializableItemUpgradeCounters serializableItemUpgradeCounters in this._upgradeCounters)
			{
				serializableItemUpgradeCounters._count = serializableItemUpgradeCounters._counters.Length;
			}
			this._upgradeCountersCount = this._upgradeCounters.Length;
			this._equipmentSlotsIds = this._equipmentSlots.Select((InventoryItemView es) => (!(es != null)) ? 0 : es._itemId).ToArray<int>();
			foreach (InventoryItemView inventoryItemView in this._itemViews)
			{
				inventoryItemView.OnSerializing();
			}
			this._inventoryGO.transform.parent = base.transform;
			yield return null;
			this._inventoryGO.transform.parent = null;
			yield break;
		}

		// Token: 0x06005674 RID: 22132 RVA: 0x0029A014 File Offset: 0x00298414
		private void ToggleInventoryItemView(int itemId, bool forceInit = false, ItemProperties properties = null)
		{
			if (this._inventoryItemViewsCache.ContainsKey(itemId))
			{
				int num = this.AmountOf(itemId, false);
				List<InventoryItemView> list = this._inventoryItemViewsCache[itemId];
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i])
					{
						InventoryItemView inventoryItemView = list[i];
						GameObject gameObject = inventoryItemView.gameObject;
						bool flag = i < num;
						if (gameObject.activeSelf != flag || forceInit)
						{
							gameObject.SetActive(flag);
							if (flag)
							{
								if (properties != ItemProperties.Any)
								{
									list[i].Properties.Copy(properties);
								}
								list[i].OnItemAdded();
							}
							else
							{
								list[i].OnItemRemoved();
							}
						}
						if (inventoryItemView.ItemCache.MatchType(Item.Types.Extension))
						{
							if (inventoryItemView._held && inventoryItemView._held.activeSelf != flag)
							{
								inventoryItemView._held.SetActive(flag);
							}
							if (this._craftingCog.ItemExtensionViewsCache.TryGetValue(itemId, out inventoryItemView) && inventoryItemView.gameObject.activeSelf != flag)
							{
								inventoryItemView.gameObject.SetActive(flag);
							}
						}
					}
				}
			}
		}

		// Token: 0x06005675 RID: 22133 RVA: 0x0029A160 File Offset: 0x00298560
		private void ThrowProjectile()
		{
			this._isThrowing = false;
			InventoryItemView inventoryItemView = this._equipmentSlots[0];
			if (inventoryItemView)
			{
				Item itemCache = inventoryItemView.ItemCache;
				bool flag = itemCache._maxAmount < 0;
				GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>((!this.UseAltWorldPrefab) ? inventoryItemView._worldPrefab : inventoryItemView._altWorldPrefab, inventoryItemView._held.transform.position, inventoryItemView._held.transform.rotation);
				Rigidbody component = gameObject.GetComponent<Rigidbody>();
				Collider component2 = gameObject.GetComponent<Collider>();
				if (BoltNetwork.isRunning)
				{
					BoltEntity component3 = gameObject.GetComponent<BoltEntity>();
					if (component3)
					{
						BoltNetwork.Attach(gameObject);
					}
				}
				if (inventoryItemView.ActiveBonus == WeaponStatUpgrade.Types.StickyProjectile)
				{
					if (component2)
					{
						gameObject.AddComponent<StickyBomb>();
						SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
						sphereCollider.isTrigger = true;
						sphereCollider.radius = 0.8f;
					}
					else
					{
						Collider componentInChildren = gameObject.GetComponentInChildren<Collider>();
						if (componentInChildren)
						{
							componentInChildren.gameObject.AddComponent<StickyBomb>();
						}
					}
				}
				if (ForestVR.Enabled)
				{
					VRThrowable component4 = inventoryItemView._held.GetComponent<VRThrowable>();
					if (component4)
					{
						component.AddForce(component4.lastThrowForce * (0.016666f / Time.fixedDeltaTime));
						component.AddTorque(component4.lastThrowForceAngular * (0.016666f / Time.fixedDeltaTime));
					}
				}
				else
				{
					component.AddForce((float)itemCache._projectileThrowForceRange * (0.016666f / Time.fixedDeltaTime) * LocalPlayer.MainCamTr.forward);
				}
				inventoryItemView._held.SendMessage("OnProjectileThrown", gameObject, SendMessageOptions.DontRequireReceiver);
				inventoryItemView.ActiveBonus = (WeaponStatUpgrade.Types)(-1);
				if (!ForestVR.Enabled)
				{
					if (itemCache._rangedStyle == Item.RangedStyle.Bell)
					{
						component.AddTorque((float)itemCache._projectileThrowTorqueRange * base.transform.forward);
					}
					else if (itemCache._rangedStyle == Item.RangedStyle.Forward)
					{
						component.AddTorque((float)itemCache._projectileThrowTorqueRange * LocalPlayer.MainCamTr.forward);
					}
				}
				if (base.transform.GetComponent<Collider>().enabled && component2 && component2.enabled)
				{
					Physics.IgnoreCollision(base.transform.GetComponent<Collider>(), component2);
				}
				if (!flag)
				{
					this.MemorizeOverrideItem(Item.EquipmentSlot.RightHand);
				}
				bool flag2 = true;
				if (LocalPlayer.FpCharacter.Sitting || LocalPlayer.AnimControl.onRope || LocalPlayer.FpCharacter.SailingRaft)
				{
					flag2 = false;
				}
				this.UnequipItemAtSlot(itemCache._equipmentSlot, false, false, flag2);
				LocalPlayer.Sfx.PlayThrow();
			}
		}

		// Token: 0x06005676 RID: 22134 RVA: 0x0029A420 File Offset: 0x00298820
		private void FireRangedWeapon()
		{
			InventoryItemView inventoryItemView = this._equipmentSlots[0];
			Item itemCache = inventoryItemView.ItemCache;
			bool flag = itemCache._maxAmount < 0;
			bool flag2 = false;
			if (flag || this.RemoveItem(itemCache._ammoItemId, 1, false, true))
			{
				InventoryItemView inventoryItemView2 = this._inventoryItemViewsCache[itemCache._ammoItemId][0];
				Item itemCache2 = inventoryItemView2.ItemCache;
				FakeParent component = inventoryItemView2._held.GetComponent<FakeParent>();
				if (this.UseAltWorldPrefab)
				{
					Debug.Log(string.Concat(new object[] { "Firing ", itemCache._name, " with '", inventoryItemView.ActiveBonus, "' ammo (alt=", this.UseAltWorldPrefab, ")" }));
				}
				GameObject gameObject;
				if (component && !component.gameObject.activeSelf)
				{
					gameObject = global::UnityEngine.Object.Instantiate<GameObject>(itemCache2._ammoPrefabs.GetPrefabForBonus(inventoryItemView.ActiveBonus, true).gameObject, component.RealPosition, component.RealRotation);
				}
				else
				{
					gameObject = global::UnityEngine.Object.Instantiate<GameObject>(itemCache2._ammoPrefabs.GetPrefabForBonus(inventoryItemView.ActiveBonus, true).gameObject, inventoryItemView2._held.transform.position, inventoryItemView2._held.transform.rotation);
				}
				if (gameObject.GetComponent<Rigidbody>())
				{
					if (itemCache.MatchRangedStyle(Item.RangedStyle.Shoot))
					{
						gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.TransformDirection(Vector3.forward * (0.016666f / Time.fixedDeltaTime) * (float)itemCache._projectileThrowForceRange), ForceMode.VelocityChange);
					}
					else
					{
						float num = Time.time - this._weaponChargeStartTime;
						if (ForestVR.Enabled)
						{
							gameObject.GetComponent<Rigidbody>().AddForce(inventoryItemView2._held.transform.up * (float)itemCache._projectileThrowForceRange);
						}
						else
						{
							gameObject.GetComponent<Rigidbody>().AddForce(inventoryItemView2._held.transform.up * Mathf.Clamp01(num / itemCache._projectileMaxChargeDuration) * (0.016666f / Time.fixedDeltaTime) * (float)itemCache._projectileThrowForceRange);
						}
						if (LocalPlayer.Inventory.HasInSlot(Item.EquipmentSlot.RightHand, LocalPlayer.AnimControl._bowId))
						{
							gameObject.SendMessage("setCraftedBowDamage", SendMessageOptions.DontRequireReceiver);
						}
					}
					inventoryItemView._held.SendMessage("OnAmmoFired", gameObject, SendMessageOptions.DontRequireReceiver);
				}
				if (itemCache._attackReleaseSFX != Item.SFXCommands.None)
				{
					LocalPlayer.Sfx.SendMessage(itemCache._attackReleaseSFX.ToString(), SendMessageOptions.DontRequireReceiver);
				}
				Mood.HitRumble();
			}
			else
			{
				flag2 = true;
				if (itemCache._dryFireSFX != Item.SFXCommands.None)
				{
					LocalPlayer.Sfx.SendMessage(itemCache._dryFireSFX.ToString(), SendMessageOptions.DontRequireReceiver);
				}
			}
			if (flag)
			{
				this.UnequipItemAtSlot(itemCache._equipmentSlot, false, false, flag);
			}
			else
			{
				this.ToggleAmmo(inventoryItemView, true);
			}
			this._weaponChargeStartTime = 0f;
			this.SetReloadDelay((!flag2) ? itemCache._reloadDuration : itemCache._dryFireReloadDuration);
			this._isThrowing = false;
		}

		// Token: 0x06005677 RID: 22135 RVA: 0x0029A760 File Offset: 0x00298B60
		public float CalculateRemainingReloadDelay()
		{
			InventoryItemView inventoryItemView = this._equipmentSlots[0];
			if (inventoryItemView && inventoryItemView.ItemCache != null)
			{
				Item itemCache = inventoryItemView.ItemCache;
				return (this._reloadingEndTime - Time.time) / itemCache._reloadDuration;
			}
			return 0f;
		}

		// Token: 0x06005678 RID: 22136 RVA: 0x0029A7AC File Offset: 0x00298BAC
		public void ForceReloadDelay()
		{
			InventoryItemView inventoryItemView = this._equipmentSlots[0];
			if (inventoryItemView && inventoryItemView.ItemCache != null)
			{
				Item itemCache = inventoryItemView.ItemCache;
				bool flag = false;
				if (itemCache._ammoItemId < 1)
				{
					flag = true;
				}
				this.SetReloadDelay((!flag) ? itemCache._reloadDuration : itemCache._dryFireReloadDuration);
			}
		}

		// Token: 0x06005679 RID: 22137 RVA: 0x0029A80C File Offset: 0x00298C0C
		public void SetReloadDelay(float delay)
		{
			this._reloadingEndTime = Time.time + delay;
		}

		// Token: 0x0600567A RID: 22138 RVA: 0x0029A81B File Offset: 0x00298C1B
		public void CancelReloadDelay()
		{
			this._reloadingEndTime = 0f;
		}

		// Token: 0x0600567B RID: 22139 RVA: 0x0029A828 File Offset: 0x00298C28
		public void UnequipItemAtSlot(Item.EquipmentSlot slot, bool drop, bool stash, bool equipPrevious)
		{
			if (!this.IsSlotEmpty(slot))
			{
				InventoryItemView inventoryItemView = this._equipmentSlots[(int)slot];
				Item itemCache = inventoryItemView.ItemCache;
				this.UnlockEquipmentSlot(slot);
				bool useAltWorldPrefab = this.UseAltWorldPrefab;
				bool flag = inventoryItemView.ItemCache.MatchType(Item.Types.Special);
				if (drop && flag)
				{
					if (this.SpecialItemsControlers[inventoryItemView._itemId].ToggleSpecial(false))
					{
						inventoryItemView._held.SetActive(false);
					}
				}
				else
				{
					inventoryItemView._held.SetActive(false);
					if (inventoryItemView.ItemCache.MatchType(Item.Types.Special))
					{
						this.SpecialItemsControlers[inventoryItemView._itemId].ToggleSpecial(false);
					}
				}
				this._itemAnimHash.ApplyAnimVars(itemCache, false);
				this._equipmentSlots[(int)slot] = ((inventoryItemView._itemId == this._defaultWeaponItemId) ? this._noEquipedItem : null);
				inventoryItemView.ApplyEquipmentEffect(false);
				if ((drop && itemCache.MatchType(Item.Types.Droppable) && !useAltWorldPrefab) || (stash && inventoryItemView.IsHeldOnly))
				{
					FakeParent component = inventoryItemView._held.GetComponent<FakeParent>();
					Vector3 vector;
					Quaternion quaternion;
					if (component && !inventoryItemView._held.transform.parent)
					{
						vector = component.RealPosition;
						quaternion = component.RealRotation;
					}
					else
					{
						vector = inventoryItemView._held.transform.position;
						quaternion = inventoryItemView._held.transform.rotation;
					}
					GameObject gameObject;
					if (!BoltNetwork.isRunning || !itemCache._pickupPrefabMP)
					{
						gameObject = global::UnityEngine.Object.Instantiate<GameObject>((!useAltWorldPrefab && inventoryItemView._worldPrefab) ? inventoryItemView._worldPrefab : ((!itemCache._pickupPrefab) ? inventoryItemView._altWorldPrefab : itemCache._pickupPrefab.gameObject), vector, quaternion);
					}
					else
					{
						gameObject = BoltNetwork.Instantiate((!useAltWorldPrefab && inventoryItemView._worldPrefab && inventoryItemView._worldPrefab.GetComponent<BoltEntity>()) ? inventoryItemView._worldPrefab : itemCache._pickupPrefabMP.gameObject, vector, quaternion);
						if (!gameObject)
						{
							gameObject = global::UnityEngine.Object.Instantiate<GameObject>((!useAltWorldPrefab && inventoryItemView._worldPrefab && inventoryItemView._worldPrefab.GetComponent<BoltEntity>()) ? inventoryItemView._worldPrefab : itemCache._pickupPrefabMP.gameObject, vector, quaternion);
						}
					}
					inventoryItemView.OnItemDropped(gameObject);
				}
				else if ((stash && itemCache._maxAmount >= 0) || (drop && (!itemCache.MatchType(Item.Types.Droppable) || useAltWorldPrefab)))
				{
					this.AddItem(inventoryItemView._itemId, 1, true, true, null);
					if (inventoryItemView.ItemCache._stashSFX != Item.SFXCommands.None)
					{
						LocalPlayer.Sfx.SendMessage(inventoryItemView.ItemCache._stashSFX.ToString(), SendMessageOptions.DontRequireReceiver);
					}
				}
				if (inventoryItemView.ItemCache._maxAmount >= 0)
				{
					this.ToggleAmmo(inventoryItemView, false);
					this.ToggleInventoryItemView(inventoryItemView._itemId, false, null);
				}
				if (equipPrevious && slot == Item.EquipmentSlot.RightHand)
				{
					this.EquipPreviousWeaponDelayed();
				}
			}
		}

		// Token: 0x0600567C RID: 22140 RVA: 0x0029AB84 File Offset: 0x00298F84
		public bool FakeDrop(int itemId, GameObject preSpawned = null)
		{
			if (itemId == this.Logs._logItemId)
			{
				return this.Logs.PutDown(true, true, true, preSpawned);
			}
			if (this._inventoryItemViewsCache.ContainsKey(itemId))
			{
				InventoryItemView inventoryItemView = this._inventoryItemViewsCache[itemId][0];
				return this.FakeDrop(inventoryItemView, false, preSpawned);
			}
			return false;
		}

		// Token: 0x0600567D RID: 22141 RVA: 0x0029ABE4 File Offset: 0x00298FE4
		public bool FakeDrop(InventoryItemView itemView, bool sendOnDropEvent, GameObject preSpawned = null)
		{
			LocalPlayer.Sfx.PlayItemCustomSfx(itemView.ItemCache, (!preSpawned) ? (LocalPlayer.Transform.position + LocalPlayer.MainCamTr.forward) : preSpawned.transform.position, true);
			if (itemView.ItemCache._pickupPrefab || itemView._worldPrefab)
			{
				GameObject gameObject = ((!itemView._held) ? this._inventoryItemViewsCache[this._defaultWeaponItemId][0]._held : itemView._held);
				FakeParent component = gameObject.GetComponent<FakeParent>();
				Vector3 vector;
				if (component && !gameObject.transform.parent)
				{
					vector = component.RealPosition + LocalPlayer.Transform.forward * 1.2f;
				}
				else
				{
					vector = gameObject.transform.position + LocalPlayer.Transform.forward * 1.2f;
				}
				if (BoltNetwork.isRunning)
				{
					BoltEntity boltEntity = ((!preSpawned) ? null : preSpawned.GetComponent<BoltEntity>());
					BoltEntity component2 = ((!itemView.ItemCache._pickupPrefabMP) ? ((!itemView.ItemCache._pickupPrefab) ? itemView._worldPrefab : itemView.ItemCache._pickupPrefab.gameObject) : itemView.ItemCache._pickupPrefabMP.gameObject).GetComponent<BoltEntity>();
					if (component2 && (!boltEntity || !boltEntity.isAttached || !boltEntity.isOwner))
					{
						DropItem dropItem = DropItem.Create(GlobalTargets.OnlyServer);
						dropItem.PrefabId = component2.prefabId;
						dropItem.Position = vector;
						dropItem.Rotation = Quaternion.identity;
						dropItem.PreSpawned = boltEntity;
						dropItem.Send();
					}
					else if (preSpawned)
					{
						preSpawned.transform.position = vector;
						preSpawned.transform.rotation = Quaternion.identity;
					}
					else
					{
						GameObject gameObject2 = global::UnityEngine.Object.Instantiate<GameObject>((!itemView.ItemCache._pickupPrefab) ? itemView._worldPrefab : itemView.ItemCache._pickupPrefab.gameObject, vector, Quaternion.identity);
						if (sendOnDropEvent)
						{
							itemView.OnItemDropped(gameObject2);
						}
					}
				}
				else if (preSpawned)
				{
					preSpawned.transform.position = vector;
					preSpawned.transform.rotation = Quaternion.identity;
				}
				else
				{
					GameObject gameObject3 = global::UnityEngine.Object.Instantiate<GameObject>((!itemView.ItemCache._pickupPrefab) ? itemView._worldPrefab : itemView.ItemCache._pickupPrefab.gameObject, vector, Quaternion.identity);
					if (sendOnDropEvent)
					{
						itemView.OnItemDropped(gameObject3);
					}
				}
				return true;
			}
			Debug.LogWarning(ItemDatabase.ItemById(itemView._itemId)._name + " doesn't have a proper pickup prefab reference and cannot be fake-dropped");
			return false;
		}

		// Token: 0x0600567E RID: 22142 RVA: 0x0029AF08 File Offset: 0x00299308
		public void ToggleAmmo(int ammoItemId, bool enable)
		{
			this._inventoryItemViewsCache[ammoItemId][0]._held.SetActive(enable && this.Owns(ammoItemId, true));
		}

		// Token: 0x0600567F RID: 22143 RVA: 0x0029AF3C File Offset: 0x0029933C
		public void ToggleAmmo(InventoryItemView itemView, bool enable)
		{
			if (itemView.ItemCache.MatchType(Item.Types.RangedWeapon) && this._inventoryItemViewsCache[itemView.ItemCache._ammoItemId][0]._held != null)
			{
				this._inventoryItemViewsCache[itemView.ItemCache._ammoItemId][0]._held.SetActive(enable && this.Owns(itemView.ItemCache._ammoItemId, true));
			}
		}

		// Token: 0x06005680 RID: 22144 RVA: 0x0029AFD0 File Offset: 0x002993D0
		public void InitItemCache()
		{
			this._itemDatabase.OnEnable();
			this._possessedItemCache = this._possessedItems.ToDictionary((InventoryItem i) => i._itemId);
			if (this._itemViews != null)
			{
				this._inventoryItemViewsCache = (from i in this._itemViews
					where i && i._itemId > 0
					select i into iv
					group iv by iv._itemId).ToDictionary((IGrouping<int, InventoryItemView> g) => g.Key, new Func<IGrouping<int, InventoryItemView>, List<InventoryItemView>>(Enumerable.ToList<InventoryItemView>));
			}
			this.ItemsUpgradeCounters = new PlayerInventory.ItemsUpgradeCountersDict();
			if (Application.isPlaying)
			{
				this._craftingCog.Awake();
				this._craftingCog.GetComponent<UpgradeCog>().Awake();
			}
		}

		// Token: 0x06005681 RID: 22145 RVA: 0x0029B0E0 File Offset: 0x002994E0
		private void RefreshDropIcon()
		{
			if (Scene.HudGui.IsNull())
			{
				return;
			}
			bool flag = (this.Logs.Amount > 0 || LocalPlayer.AnimControl.carry || (!this.IsRightHandEmpty() && this._equipmentSlots[0].ItemCache._maxAmount < 0)) && !this.DontShowDrop && this.CurrentView == PlayerInventory.PlayerViews.World;
			if (Scene.HudGui.DropButton.activeSelf != flag)
			{
				Scene.HudGui.DropButton.SetActive(flag);
			}
		}

		// Token: 0x170008AD RID: 2221
		// (get) Token: 0x06005682 RID: 22146 RVA: 0x0029B17D File Offset: 0x0029957D
		public Dictionary<int, List<InventoryItemView>> InventoryItemViewsCache
		{
			get
			{
				return this._inventoryItemViewsCache;
			}
		}

		// Token: 0x170008AE RID: 2222
		// (get) Token: 0x06005683 RID: 22147 RVA: 0x0029B185 File Offset: 0x00299585
		// (set) Token: 0x06005684 RID: 22148 RVA: 0x0029B18D File Offset: 0x0029958D
		public Dictionary<int, SpecialItemControlerBase> SpecialItemsControlers { get; set; }

		// Token: 0x170008AF RID: 2223
		// (get) Token: 0x06005685 RID: 22149 RVA: 0x0029B196 File Offset: 0x00299596
		public GameObject SpecialItems
		{
			get
			{
				return this._specialItems;
			}
		}

		// Token: 0x170008B0 RID: 2224
		// (get) Token: 0x06005686 RID: 22150 RVA: 0x0029B19E File Offset: 0x0029959E
		public GameObject SpecialActions
		{
			get
			{
				return this._specialActions;
			}
		}

		// Token: 0x170008B1 RID: 2225
		// (get) Token: 0x06005687 RID: 22151 RVA: 0x0029B1A6 File Offset: 0x002995A6
		public PlayMakerFSM PM
		{
			get
			{
				return this._pm;
			}
		}

		// Token: 0x170008B2 RID: 2226
		// (get) Token: 0x06005688 RID: 22152 RVA: 0x0029B1AE File Offset: 0x002995AE
		// (set) Token: 0x06005689 RID: 22153 RVA: 0x0029B1B6 File Offset: 0x002995B6
		public LighterControler DefaultLight { get; set; }

		// Token: 0x170008B3 RID: 2227
		// (get) Token: 0x0600568A RID: 22154 RVA: 0x0029B1BF File Offset: 0x002995BF
		// (set) Token: 0x0600568B RID: 22155 RVA: 0x0029B1C7 File Offset: 0x002995C7
		public SpecialItemControlerBase LastLight { get; set; }

		// Token: 0x170008B4 RID: 2228
		// (get) Token: 0x0600568C RID: 22156 RVA: 0x0029B1D0 File Offset: 0x002995D0
		// (set) Token: 0x0600568D RID: 22157 RVA: 0x0029B1D8 File Offset: 0x002995D8
		public SpecialItemControlerBase LastUtility { get; set; }

		// Token: 0x170008B5 RID: 2229
		// (get) Token: 0x0600568E RID: 22158 RVA: 0x0029B1E1 File Offset: 0x002995E1
		public ItemAnimatorHashHelper ItemAnimHash
		{
			get
			{
				return this._itemAnimHash;
			}
		}

		// Token: 0x170008B6 RID: 2230
		// (get) Token: 0x0600568F RID: 22159 RVA: 0x0029B1E9 File Offset: 0x002995E9
		public InventoryItemView[] EquipmentSlots
		{
			get
			{
				return this._equipmentSlots;
			}
		}

		// Token: 0x170008B7 RID: 2231
		// (get) Token: 0x06005690 RID: 22160 RVA: 0x0029B1F1 File Offset: 0x002995F1
		public InventoryItemView[] EquipmentSlotsPrevious
		{
			get
			{
				return this._equipmentSlotsPrevious;
			}
		}

		// Token: 0x170008B8 RID: 2232
		// (get) Token: 0x06005691 RID: 22161 RVA: 0x0029B1F9 File Offset: 0x002995F9
		public InventoryItemView LeftHand
		{
			get
			{
				return this._equipmentSlots[1];
			}
		}

		// Token: 0x170008B9 RID: 2233
		// (get) Token: 0x06005692 RID: 22162 RVA: 0x0029B203 File Offset: 0x00299603
		public InventoryItemView RightHand
		{
			get
			{
				return this._equipmentSlots[0];
			}
		}

		// Token: 0x170008BA RID: 2234
		// (get) Token: 0x06005693 RID: 22163 RVA: 0x0029B20D File Offset: 0x0029960D
		public InventoryItemView RightHandOrNext
		{
			get
			{
				return (!this.IsSlotNextEmpty(Item.EquipmentSlot.RightHand)) ? this._equipmentSlotsNext[0] : this._equipmentSlots[0];
			}
		}

		// Token: 0x170008BB RID: 2235
		// (get) Token: 0x06005694 RID: 22164 RVA: 0x0029B230 File Offset: 0x00299630
		public int[] QuickSelectItemIds
		{
			get
			{
				return this._quickSelectItemIds;
			}
		}

		// Token: 0x170008BC RID: 2236
		// (get) Token: 0x06005695 RID: 22165 RVA: 0x0029B238 File Offset: 0x00299638
		// (set) Token: 0x06005696 RID: 22166 RVA: 0x0029B240 File Offset: 0x00299640
		public LogControler Logs { get; set; }

		// Token: 0x170008BD RID: 2237
		// (get) Token: 0x06005697 RID: 22167 RVA: 0x0029B249 File Offset: 0x00299649
		// (set) Token: 0x06005698 RID: 22168 RVA: 0x0029B251 File Offset: 0x00299651
		public PlayerInventory.ItemsUpgradeCountersDict ItemsUpgradeCounters { get; set; }

		// Token: 0x170008BE RID: 2238
		// (get) Token: 0x06005699 RID: 22169 RVA: 0x0029B25A File Offset: 0x0029965A
		// (set) Token: 0x0600569A RID: 22170 RVA: 0x0029B262 File Offset: 0x00299662
		public bool BlockTogglingInventory { get; set; }

		// Token: 0x170008BF RID: 2239
		// (get) Token: 0x0600569B RID: 22171 RVA: 0x0029B26B File Offset: 0x0029966B
		// (set) Token: 0x0600569C RID: 22172 RVA: 0x0029B273 File Offset: 0x00299673
		public bool QuickSelectGamepadSwitch { get; set; }

		// Token: 0x170008C0 RID: 2240
		// (get) Token: 0x0600569D RID: 22173 RVA: 0x0029B27C File Offset: 0x0029967C
		// (set) Token: 0x0600569E RID: 22174 RVA: 0x0029B284 File Offset: 0x00299684
		public bool IsWeaponBurning { get; set; }

		// Token: 0x170008C1 RID: 2241
		// (get) Token: 0x0600569F RID: 22175 RVA: 0x0029B28D File Offset: 0x0029968D
		// (set) Token: 0x060056A0 RID: 22176 RVA: 0x0029B295 File Offset: 0x00299695
		public bool CancelNextChargedAttack { get; set; }

		// Token: 0x170008C2 RID: 2242
		// (get) Token: 0x060056A1 RID: 22177 RVA: 0x0029B29E File Offset: 0x0029969E
		// (set) Token: 0x060056A2 RID: 22178 RVA: 0x0029B2A6 File Offset: 0x002996A6
		public bool SkipNextAddItemWoosh { get; set; }

		// Token: 0x170008C3 RID: 2243
		// (get) Token: 0x060056A3 RID: 22179 RVA: 0x0029B2AF File Offset: 0x002996AF
		// (set) Token: 0x060056A4 RID: 22180 RVA: 0x0029B2B7 File Offset: 0x002996B7
		public bool DontShowDrop { get; set; }

		// Token: 0x170008C4 RID: 2244
		// (get) Token: 0x060056A5 RID: 22181 RVA: 0x0029B2C0 File Offset: 0x002996C0
		// (set) Token: 0x060056A6 RID: 22182 RVA: 0x0029B2C8 File Offset: 0x002996C8
		public bool BlockDrop { get; set; }

		// Token: 0x170008C5 RID: 2245
		// (get) Token: 0x060056A7 RID: 22183 RVA: 0x0029B2D1 File Offset: 0x002996D1
		// (set) Token: 0x060056A8 RID: 22184 RVA: 0x0029B2D9 File Offset: 0x002996D9
		public string PendingSendMessage { get; set; }

		// Token: 0x170008C6 RID: 2246
		// (get) Token: 0x060056A9 RID: 22185 RVA: 0x0029B2E2 File Offset: 0x002996E2
		public float WeaponChargeStartTime
		{
			get
			{
				return this._weaponChargeStartTime;
			}
		}

		// Token: 0x170008C7 RID: 2247
		// (get) Token: 0x060056AA RID: 22186 RVA: 0x0029B2EA File Offset: 0x002996EA
		public bool IsReloading
		{
			get
			{
				return this._reloadingEndTime > Time.time;
			}
		}

		// Token: 0x170008C8 RID: 2248
		// (get) Token: 0x060056AB RID: 22187 RVA: 0x0029B2F9 File Offset: 0x002996F9
		public bool IsThrowing
		{
			get
			{
				return this._isThrowing;
			}
		}

		// Token: 0x170008C9 RID: 2249
		// (get) Token: 0x060056AC RID: 22188 RVA: 0x0029B301 File Offset: 0x00299701
		// (set) Token: 0x060056AD RID: 22189 RVA: 0x0029B309 File Offset: 0x00299709
		public bool UseAltWorldPrefab { get; set; }

		// Token: 0x170008CA RID: 2250
		// (get) Token: 0x060056AE RID: 22190 RVA: 0x0029B312 File Offset: 0x00299712
		// (set) Token: 0x060056AF RID: 22191 RVA: 0x0029B31A File Offset: 0x0029971A
		public bool IsOpenningInventory { get; private set; }

		// Token: 0x170008CB RID: 2251
		// (get) Token: 0x060056B0 RID: 22192 RVA: 0x0029B323 File Offset: 0x00299723
		// (set) Token: 0x060056B1 RID: 22193 RVA: 0x0029B32B File Offset: 0x0029972B
		public bool blockRangedAttack { get; set; }

		// Token: 0x170008CC RID: 2252
		// (get) Token: 0x060056B2 RID: 22194 RVA: 0x0029B334 File Offset: 0x00299734
		// (set) Token: 0x060056B3 RID: 22195 RVA: 0x0029B33C File Offset: 0x0029973C
		public IItemStorage CurrentStorage { get; private set; }

		// Token: 0x170008CD RID: 2253
		// (get) Token: 0x060056B4 RID: 22196 RVA: 0x0029B345 File Offset: 0x00299745
		// (set) Token: 0x060056B5 RID: 22197 RVA: 0x0029B34D File Offset: 0x0029974D
		public IInventoryItemFilter ItemFilter { get; set; }

		// Token: 0x170008CE RID: 2254
		// (get) Token: 0x060056B6 RID: 22198 RVA: 0x0029B356 File Offset: 0x00299756
		// (set) Token: 0x060056B7 RID: 22199 RVA: 0x0029B360 File Offset: 0x00299760
		public PlayerInventory.PlayerViews CurrentView
		{
			get
			{
				return this._currentView;
			}
			set
			{
				if (LocalPlayer.Stats.Dead && value != PlayerInventory.PlayerViews.Pause)
				{
					this._currentView = ((value != PlayerInventory.PlayerViews.WakingUp) ? PlayerInventory.PlayerViews.Death : value);
				}
				else
				{
					this._currentView = value;
				}
				if (BoltNetwork.isRunning && LocalPlayer.Entity && LocalPlayer.Entity.IsAttached())
				{
					if (LocalPlayer.Stats.Dead)
					{
						LocalPlayer.Entity.GetState<IPlayerState>().CurrentView = (int)((value != PlayerInventory.PlayerViews.WakingUp) ? PlayerInventory.PlayerViews.Death : value);
					}
					else
					{
						LocalPlayer.Entity.GetState<IPlayerState>().CurrentView = (int)value;
					}
				}
			}
		}

		// Token: 0x04005C34 RID: 23604
		public ItemDatabase _itemDatabase;

		// Token: 0x04005C35 RID: 23605
		[SerializeThis]
		public List<InventoryItem> _possessedItems;

		// Token: 0x04005C36 RID: 23606
		[SerializeThis]
		public int _possessedItemsCount;

		// Token: 0x04005C37 RID: 23607
		public CraftingCog _craftingCog;

		// Token: 0x04005C38 RID: 23608
		public List<UpgradeViewReceiver> _upgradeViewReceivers;

		// Token: 0x04005C39 RID: 23609
		public InventoryItemView[] _itemViews;

		// Token: 0x04005C3A RID: 23610
		public GameObject _specialActions;

		// Token: 0x04005C3B RID: 23611
		public GameObject _specialItems;

		// Token: 0x04005C3C RID: 23612
		public GameObject _inventoryGO;

		// Token: 0x04005C3D RID: 23613
		[ItemIdPicker]
		public int _leafItemId;

		// Token: 0x04005C3E RID: 23614
		[ItemIdPicker]
		public int _seedItemId;

		// Token: 0x04005C3F RID: 23615
		[ItemIdPicker]
		public int _sapItemId;

		// Token: 0x04005C40 RID: 23616
		[ItemIdPicker(Item.Types.Equipment)]
		public int _defaultWeaponItemId;

		// Token: 0x04005C41 RID: 23617
		private PlayerInventory.PlayerViews _currentView;

		// Token: 0x04005C42 RID: 23618
		[SerializeThis]
		private int[] _equipmentSlotsIds;

		// Token: 0x04005C43 RID: 23619
		private bool[] _equipmentSlotsLocked;

		// Token: 0x04005C44 RID: 23620
		private InventoryItemView[] _equipmentSlots;

		// Token: 0x04005C45 RID: 23621
		private InventoryItemView[] _equipmentSlotsPrevious;

		// Token: 0x04005C46 RID: 23622
		private InventoryItemView[] _equipmentSlotsPreviousOverride;

		// Token: 0x04005C47 RID: 23623
		private InventoryItemView[] _equipmentSlotsNext;

		// Token: 0x04005C48 RID: 23624
		private InventoryItemView _noEquipedItem;

		// Token: 0x04005C49 RID: 23625
		[SerializeThis]
		private PlayerInventory.SerializableItemUpgradeCounters[] _upgradeCounters;

		// Token: 0x04005C4A RID: 23626
		[SerializeThis]
		private int _upgradeCountersCount;

		// Token: 0x04005C4B RID: 23627
		private Dictionary<int, InventoryItem> _possessedItemCache;

		// Token: 0x04005C4C RID: 23628
		private Dictionary<int, List<InventoryItemView>> _inventoryItemViewsCache;

		// Token: 0x04005C4D RID: 23629
		private PlayMakerFSM _pm;

		// Token: 0x04005C4E RID: 23630
		private ItemAnimatorHashHelper _itemAnimHash;

		// Token: 0x04005C4F RID: 23631
		private float _weaponChargeStartTime;

		// Token: 0x04005C50 RID: 23632
		private float _equipPreviousTime;

		// Token: 0x04005C51 RID: 23633
		public float _reloadingEndTime;

		// Token: 0x04005C52 RID: 23634
		private bool _isThrowing;

		// Token: 0x04005C53 RID: 23635
		private EventInstance _pauseSnapshot;

		// Token: 0x04005C54 RID: 23636
		[SerializeThis]
		[ItemIdPicker]
		private int[] _quickSelectItemIds;

		// Token: 0x04005C55 RID: 23637
		private string[] _quickSelectButtons = new string[] { "ItemSlot1", "ItemSlot2", "ItemSlot3", "ItemSlot4" };

		// Token: 0x04005C56 RID: 23638
		private bool _pendingEquip;

		// Token: 0x04005C57 RID: 23639
		public UnityEvent StashedLeftHand = new UnityEvent();

		// Token: 0x04005C58 RID: 23640
		public UnityEvent StashedRightHand = new UnityEvent();

		// Token: 0x04005C59 RID: 23641
		public UnityEvent DroppedRightHand = new UnityEvent();

		// Token: 0x04005C5A RID: 23642
		public UnityEvent Attacked = new UnityEvent();

		// Token: 0x04005C5B RID: 23643
		public UnityEvent AttackEnded = new UnityEvent();

		// Token: 0x04005C5C RID: 23644
		public UnityEvent ReleasedAttack = new UnityEvent();

		// Token: 0x04005C5D RID: 23645
		public UnityEvent Blocked = new UnityEvent();

		// Token: 0x04005C5E RID: 23646
		public UnityEvent Unblocked = new UnityEvent();

		// Token: 0x02000CAA RID: 3242
		public class ItemEvent : UnityEvent<int>
		{
		}

		// Token: 0x02000CAB RID: 3243
		public enum PlayerViews
		{
			// Token: 0x04005C78 RID: 23672
			PlaneCrash = -1,
			// Token: 0x04005C79 RID: 23673
			Loading,
			// Token: 0x04005C7A RID: 23674
			WakingUp,
			// Token: 0x04005C7B RID: 23675
			World,
			// Token: 0x04005C7C RID: 23676
			Inventory,
			// Token: 0x04005C7D RID: 23677
			ClosingInventory,
			// Token: 0x04005C7E RID: 23678
			Book,
			// Token: 0x04005C7F RID: 23679
			Pause,
			// Token: 0x04005C80 RID: 23680
			Death,
			// Token: 0x04005C81 RID: 23681
			Loot,
			// Token: 0x04005C82 RID: 23682
			Sleep,
			// Token: 0x04005C83 RID: 23683
			EndCrash,
			// Token: 0x04005C84 RID: 23684
			PlayerList,
			// Token: 0x04005C85 RID: 23685
			EndDeactivate
		}

		// Token: 0x02000CAC RID: 3244
		public class ItemsUpgradeCountersDict : Dictionary<int, PlayerInventory.UpgradeCounterDict>
		{
		}

		// Token: 0x02000CAD RID: 3245
		public class UpgradeCounterDict : Dictionary<int, int>
		{
		}

		// Token: 0x02000CAE RID: 3246
		[Serializable]
		public class SerializableItemUpgradeCounters
		{
			// Token: 0x04005C86 RID: 23686
			public int _itemId;

			// Token: 0x04005C87 RID: 23687
			public int _count;

			// Token: 0x04005C88 RID: 23688
			public PlayerInventory.SerializableUpgradeCounter[] _counters;
		}

		// Token: 0x02000CAF RID: 3247
		[Serializable]
		public class SerializableUpgradeCounter
		{
			// Token: 0x04005C89 RID: 23689
			public int _upgradeItemId;

			// Token: 0x04005C8A RID: 23690
			public int _amount;
		}
	}
}
