using System;
using TheForest.Items.Craft;
using TheForest.Items.Inventory;
using TheForest.Items.Special;
using TheForest.Items.World.Interfaces;
using TheForest.Player;
using TheForest.Tools;
using TheForest.Utils;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace TheForest.Items.World
{
	// Token: 0x02000CD6 RID: 3286
	public class BowController : MonoBehaviour, IBurnableItem
	{
		// Token: 0x060057AD RID: 22445 RVA: 0x002A376C File Offset: 0x002A1B6C
		private void Start()
		{
			if (base.GetComponentInParent<PlayerInventory>())
			{
				this._nextReArm = float.MaxValue;
				this._ammoAnimated.SetActive(true);
				this._attackHash = Animator.StringToHash("attacking");
				this._animator = LocalPlayer.Animator;
				this._bowAnimator = base.GetComponent<Animator>();
				if (this._aimingReticle)
				{
					this._aimingReticle.enabled = false;
				}
				base.Invoke("UpdateArrowRenderer", 0.25f);
			}
		}

		// Token: 0x060057AE RID: 22446 RVA: 0x002A37F4 File Offset: 0x002A1BF4
		private void OnEnable()
		{
			if (ForestVR.Enabled)
			{
				if (this._bowVr)
				{
					if (this._bowVr)
					{
						this._bowVr.SetActive(true);
					}
					this._bowVr.SendMessage("SpawnAndAttachObject", LocalPlayer.vrPlayerControl.RightHand);
				}
				if (this._bowAnimator)
				{
					this._bowAnimator.SetBool("VR", true);
				}
				this._prevHoverRadius = LocalPlayer.vrPlayerControl.LeftHand.hoverSphereRadius;
				LocalPlayer.vrPlayerControl.LeftHand.hoverSphereRadius = 0.2f;
			}
			else if (this._bowVr)
			{
				this._bowVr.SetActive(false);
			}
			EventRegistry.Player.Subscribe(TfEvent.AddedItem, new EventRegistry.SubscriberCallback(this.OnItemAdded));
			if (this.CurrentArrowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo)
			{
				LighterControler.HasLightableItem = true;
			}
			this.SetActiveArrowBonus(this._activeAmmoBonus);
			this.UpdateArrowRenderer();
			LocalPlayer.ActiveBurnableItem = this;
		}

		// Token: 0x060057AF RID: 22447 RVA: 0x002A3908 File Offset: 0x002A1D08
		private void OnDisable()
		{
			if (ForestVR.Enabled)
			{
				if (this._bowVr)
				{
					ItemPackageSpawner component = this._bowVr.GetComponent<ItemPackageSpawner>();
					if (component)
					{
						component.removeAttachedObjects(LocalPlayer.vrPlayerControl.RightHand);
					}
				}
				LocalPlayer.vrPlayerControl.LeftHand.hoverSphereRadius = this._prevHoverRadius;
			}
			EventRegistry.Player.Unsubscribe(TfEvent.AddedItem, new EventRegistry.SubscriberCallback(this.OnItemAdded));
			LighterControler.HasLightableItem = false;
			this._nextReArm = float.MaxValue;
			this._ammoAnimated.SetActive(true);
			if (this._animator)
			{
				this._animator.SetBoolReflected("drawBowBool", false);
				if (base.gameObject.activeInHierarchy)
				{
					this._bowAnimator.SetBoolReflected("drawBool", false);
				}
				this.ShutDown(false);
				this.ShutDownFire();
			}
			if (this._activeFireArrowGO)
			{
				global::UnityEngine.Object.Destroy(this._activeFireArrowGO);
				LocalPlayer.Inventory.IsWeaponBurning = false;
				LocalPlayer.ScriptSetup.targetInfo.arrowFire = false;
			}
			this._lightingArrow = false;
			if (Scene.HudGui)
			{
				Scene.HudGui.ToggleArrowBonusIcon.SetActive(false);
			}
			if (this.Equals(LocalPlayer.ActiveBurnableItem))
			{
				LocalPlayer.ActiveBurnableItem = null;
			}
		}

		// Token: 0x060057B0 RID: 22448 RVA: 0x002A3A64 File Offset: 0x002A1E64
		private void Update()
		{
			if (this._player.CurrentView == PlayerInventory.PlayerViews.World)
			{
				if (ForestVR.Enabled)
				{
					if (this._longbowVr == null)
					{
						this._longbowVr = LocalPlayer.vrPlayerControl.VRCameraRig.GetComponentInChildren<Longbow>();
					}
					if (this._arrowHandVr == null)
					{
						this._arrowHandVr = LocalPlayer.vrPlayerControl.VRCameraRig.GetComponentInChildren<ArrowHand>();
					}
					if (this._bowAnimatorVr == null && this._longbowVr)
					{
						this._bowAnimatorVr = this._longbowVr.GetComponent<Animator>();
					}
					LocalPlayer.Inventory.CancelNextChargedAttack = false;
				}
				if (this._player.Owns(this._ammoItemId, false))
				{
					LocalPlayer.Animator.SetBool("noAmmo", false);
				}
				else
				{
					LocalPlayer.Animator.SetBool("noAmmo", true);
				}
				this._bowAnimator.SetFloat("bowSpeed", this._bowSpeed);
				if (LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBowHash && !LocalPlayer.Animator.IsInTransition(1) && !ForestVR.Enabled)
				{
					this._ammoAnimated.transform.position = LocalPlayer.ScriptSetup.leftHandHeld.position;
				}
				if (!LocalPlayer.Create.Grabber.Target && LocalPlayer.MainCamTr.forward.y < -0.85f && !LocalPlayer.Animator.GetBool("drawBowBool"))
				{
					WeaponStatUpgrade.Types types = this.NextAvailableArrowBonus(this.BowItemView.ActiveBonus);
					if (types != this.BowItemView.ActiveBonus)
					{
						this._showRotateArrowType = true;
						if (!Scene.HudGui.ToggleArrowBonusIcon.activeSelf)
						{
							Scene.HudGui.ToggleArrowBonusIcon.SetActive(true);
						}
						if (TheForest.Utils.Input.GetButtonDown("Rotate"))
						{
							LocalPlayer.Sfx.PlayWhoosh();
							this.SetActiveBowBonus(types);
							Scene.HudGui.ToggleArrowBonusIcon.SetActive(false);
						}
					}
					else if (this._showRotateArrowType)
					{
						this._showRotateArrowType = false;
						Scene.HudGui.ToggleArrowBonusIcon.SetActive(false);
					}
				}
				else if (this._showRotateArrowType)
				{
					this._showRotateArrowType = false;
					Scene.HudGui.ToggleArrowBonusIcon.SetActive(false);
				}
				if (this.CurrentArrowItemView.ActiveBonus != this.BowItemView.ActiveBonus)
				{
					LocalPlayer.Inventory.SortInventoryViewsByBonus(this.CurrentArrowItemView, this.BowItemView.ActiveBonus, false);
					if (this.CurrentArrowItemView.ActiveBonus != this.BowItemView.ActiveBonus)
					{
						this.SetActiveBowBonus(this.CurrentArrowItemView.ActiveBonus);
					}
					this.UpdateArrowRenderer();
				}
				WeaponStatUpgrade.Types activeBonus = this.CurrentArrowItemView.ActiveBonus;
				bool canSetArrowOnFire = this.CanSetArrowOnFire;
				if (canSetArrowOnFire)
				{
					if (TheForest.Utils.Input.GetButtonAfterDelay("Lighter", 0.5f, false) && !ForestVR.Enabled)
					{
						this.SetArrowOnFire();
					}
				}
				else if (activeBonus != WeaponStatUpgrade.Types.BurningAmmo && this._activeAmmoBonus != activeBonus)
				{
					this.SetActiveArrowBonus(activeBonus);
				}
				if (!this._lightingArrow)
				{
					AnimatorStateInfo currentAnimatorStateInfo = LocalPlayer.Animator.GetCurrentAnimatorStateInfo(1);
					if (TheForest.Utils.Input.GetButtonDown("Fire1") && !LocalPlayer.Animator.GetBool("ballHeld") && !ForestVR.Enabled)
					{
						LocalPlayer.Inventory.CancelNextChargedAttack = false;
						if (this._aimingReticle)
						{
							this._aimingReticle.enabled = true;
						}
						if ((currentAnimatorStateInfo.shortNameHash != this._lightBowHash || currentAnimatorStateInfo.normalizedTime >= 0.95f) && currentAnimatorStateInfo.shortNameHash != this._releaseBow0Hash && (currentAnimatorStateInfo.shortNameHash != this._releaseBowHash || currentAnimatorStateInfo.normalizedTime >= 0.5f))
						{
							this.ReArm();
						}
						this._animator.SetBoolReflected("drawBowBool", true);
						this._bowAnimator.SetBoolReflected("drawBool", true);
						this._bowAnimator.SetBoolReflected("bowFireBool", false);
						this._animator.SetBoolReflected("bowFireBool", false);
						this._animator.SetBoolReflected("lightWeaponBool", false);
						LocalPlayer.SpecialItems.SendMessage("cancelLightingFromBow");
						LocalPlayer.Inventory.UnlockEquipmentSlot(Item.EquipmentSlot.LeftHand);
						this._player.StashLeftHand();
						this._animator.SetBoolReflected("checkArms", false);
						this._animator.SetBoolReflected("onHand", false);
					}
					else if ((TheForest.Utils.Input.GetButtonDown("AltFire") || LocalPlayer.Animator.GetBool("ballHeld")) && !ForestVR.Enabled)
					{
						LocalPlayer.AnimControl.animEvents.enableSpine();
						this._player.CancelNextChargedAttack = true;
						this._animator.SetBool("drawBowBool", false);
						this._bowAnimator.SetBool("drawBool", false);
						this.ShutDown(false);
					}
					if (currentAnimatorStateInfo.shortNameHash == this._drawIdleHash && !LocalPlayer.Inventory.IsLeftHandEmpty())
					{
						LocalPlayer.SpecialItems.SendMessage("cancelLightingFromBow");
						LocalPlayer.Inventory.UnlockEquipmentSlot(Item.EquipmentSlot.LeftHand);
						this._player.StashLeftHand();
					}
					if (currentAnimatorStateInfo.shortNameHash == this._drawBowHash && !ForestVR.Enabled)
					{
						this._bowAnimator.Play(this._drawBowHash, 0, currentAnimatorStateInfo.normalizedTime);
					}
					if ((TheForest.Utils.Input.GetButtonUp("Fire1") || LocalPlayer.Animator.GetBool("ballHeld")) && !ForestVR.Enabled)
					{
						this._currentAmmo = this.CurrentArrowItemView;
						if (this._aimingReticle)
						{
							this._aimingReticle.enabled = false;
						}
						base.CancelInvoke();
						if (this._animator.GetCurrentAnimatorStateInfo(1).tagHash == this._attackHash && this._animator.GetBool("drawBowBool") && !LocalPlayer.Animator.GetBool("ballHeld") && !LocalPlayer.Inventory.blockRangedAttack && LocalPlayer.AnimControl.currLayerState0.shortNameHash != LocalPlayer.AnimControl.landHeavyHash)
						{
							this._animator.SetBoolReflected("bowFireBool", true);
							this._bowAnimator.SetBoolReflected("bowFireBool", true);
							this._animator.SetBoolReflected("drawBowBool", false);
							this._bowAnimator.SetBoolReflected("drawBool", false);
							LocalPlayer.TargetFunctions.sendPlayerAttacking();
							this.InitReArm();
						}
						else if (LocalPlayer.Animator.GetBool("ballHeld"))
						{
							LocalPlayer.AnimControl.animEvents.enableSpine();
							this._player.CancelNextChargedAttack = true;
							this._animator.SetBoolReflected("drawBowBool", false);
							this._bowAnimator.SetBoolReflected("drawBool", false);
							this.ShutDown(false);
						}
						else if ((LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBow0Hash || LocalPlayer.AnimControl.currLayerState0.shortNameHash == LocalPlayer.AnimControl.landHeavyHash || LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBowHash || LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._drawBowHash || LocalPlayer.AnimControl.nextLayerState1.shortNameHash == this._drawBowHash) && LocalPlayer.AnimControl.nextLayerState1.shortNameHash != this._bowIdleHash)
						{
							if (LocalPlayer.Inventory.blockRangedAttack)
							{
								this.ShutDown(false);
							}
							else
							{
								this.ShutDown(true);
							}
						}
					}
					else if (this._nextReArm < Time.time)
					{
						this.ReArm();
					}
				}
				else if (!ForestVR.Enabled)
				{
					LocalPlayer.Inventory.CancelNextChargedAttack = true;
				}
			}
		}

		// Token: 0x060057B1 RID: 22449 RVA: 0x002A4270 File Offset: 0x002A2670
		private void LateUpdate()
		{
			if (LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBowHash && !LocalPlayer.Animator.IsInTransition(1) && !ForestVR.Enabled)
			{
				this._ammoAnimated.transform.position = LocalPlayer.ScriptSetup.leftHandHeld.position;
			}
			if (ForestVR.Enabled)
			{
				if (this._longbowVr)
				{
					base.transform.position = this._longbowVr.bowFollowTransform.transform.position;
					base.transform.rotation = this._longbowVr.bowFollowTransform.transform.rotation;
					this._bowAnimator.Play(this._bowPullVrHash, 0, this._bowAnimatorVr.GetCurrentAnimatorStateInfo(0).normalizedTime);
				}
				if (this._arrowHandVr)
				{
					if (this._arrowHandVr.currentArrow != null)
					{
						this._ammoAnimated.transform.position = this._arrowHandVr.currentArrow.GetComponent<Arrow>().arrowFollowTransform.position;
						this._ammoAnimated.transform.rotation = this._arrowHandVr.currentArrow.GetComponent<Arrow>().arrowFollowTransform.rotation;
						this._ammoAnimated.SetActive(true);
					}
					else
					{
						this._ammoAnimated.SetActive(false);
					}
				}
			}
		}

		// Token: 0x060057B2 RID: 22450 RVA: 0x002A43E7 File Offset: 0x002A27E7
		private void OnDestroy()
		{
			if (this._ammoAnimated)
			{
				global::UnityEngine.Object.Destroy(this._ammoAnimated);
			}
		}

		// Token: 0x060057B3 RID: 22451 RVA: 0x002A4404 File Offset: 0x002A2804
		private void OnItemAdded(object o)
		{
			this.OnItemAdded((int)o);
		}

		// Token: 0x060057B4 RID: 22452 RVA: 0x002A4412 File Offset: 0x002A2812
		private void OnItemAdded(int itemId)
		{
			if (this._ammoItemId == itemId && this._player.AmountOf(this._ammoItemId, false) == 1)
			{
				this._player.ToggleAmmo(this._ammoItemId, true);
			}
		}

		// Token: 0x060057B5 RID: 22453 RVA: 0x002A444A File Offset: 0x002A284A
		private void ShutDownFire()
		{
			this.SetActiveArrowBonus((WeaponStatUpgrade.Types)(-1));
			LocalPlayer.Inventory.IsWeaponBurning = false;
			LocalPlayer.ScriptSetup.targetInfo.arrowFire = false;
		}

		// Token: 0x060057B6 RID: 22454 RVA: 0x002A4470 File Offset: 0x002A2870
		private void ShutDown(bool rearm)
		{
			base.CancelInvoke();
			if (base.gameObject.activeInHierarchy)
			{
				this._animator.SetBool("drawBowBool", false);
				this._bowAnimator.SetBool("drawBool", false);
				this._animator.SetBool("bowFireBool", false);
				this._bowAnimator.SetBool("bowFireBool", false);
			}
			if (rearm)
			{
				this.InitReArm();
			}
		}

		// Token: 0x060057B7 RID: 22455 RVA: 0x002A44E4 File Offset: 0x002A28E4
		private void InitReArm()
		{
			if (this._activeFireArrowGO)
			{
				this._activeFireArrowGO.transform.parent = null;
			}
			this._ammoAnimated.SetActive(false);
			this._nextReArm = Time.time + this._reArmDelay;
		}

		// Token: 0x060057B8 RID: 22456 RVA: 0x002A4530 File Offset: 0x002A2930
		private void ReArm()
		{
			this._nextReArm = float.MaxValue;
			this.EnsureArrowIsInHierchy();
			this._ammoAnimated.SetActive(true);
		}

		// Token: 0x060057B9 RID: 22457 RVA: 0x002A4550 File Offset: 0x002A2950
		private void EnsureArrowIsInHierchy()
		{
			if (!Reparent.Locked && this._ammoAnimationRenderer.transform.parent.parent != this._ammoHook && base.transform.root.CompareTag("Player"))
			{
				this._ammoAnimationRenderer.transform.parent.parent = this._ammoHook;
				this._ammoAnimationRenderer.transform.parent.localPosition = Vector3.zero;
				this._ammoAnimationRenderer.transform.parent.localRotation = Quaternion.identity;
			}
		}

		// Token: 0x060057BA RID: 22458 RVA: 0x002A45F5 File Offset: 0x002A29F5
		private void LightArrowVR()
		{
			if (this.CanSetArrowOnFire)
			{
				this.LightArrow();
			}
		}

		// Token: 0x060057BB RID: 22459 RVA: 0x002A4608 File Offset: 0x002A2A08
		private void LightArrow()
		{
			GameStats.LitArrow.Invoke();
			this._lightingArrow = false;
			this.SetActiveArrowBonus(WeaponStatUpgrade.Types.BurningAmmo);
			this._activeFireArrowGO = global::UnityEngine.Object.Instantiate<MasterFireSpread>(this._fireArrowPrefab);
			this._activeFireArrowGO.enabled = false;
			this._activeFireArrowGO.transform.parent = this._ammoAnimated.transform;
			this._activeFireArrowGO.transform.position = this._ammoAnimationRenderer.transform.position;
			this._activeFireArrowGO.transform.rotation = this._ammoAnimationRenderer.transform.rotation;
			this._activeFireArrowGO.owner = LocalPlayer.Transform;
			WeaponBonus componentInChildren = this._activeFireArrowGO.GetComponentInChildren<WeaponBonus>();
			if (componentInChildren)
			{
				componentInChildren._owner = LocalPlayer.Transform;
				componentInChildren.enabled = true;
			}
			LocalPlayer.Inventory.IsWeaponBurning = true;
			LocalPlayer.ScriptSetup.targetInfo.arrowFire = true;
			LighterControler.HasLightableItem = this.CurrentArrowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo;
		}

		// Token: 0x060057BC RID: 22460 RVA: 0x002A4710 File Offset: 0x002A2B10
		private void SetActiveBowBonus(WeaponStatUpgrade.Types bonusType)
		{
			if (this.BowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo && this.CurrentArrowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo && bonusType != WeaponStatUpgrade.Types.BurningAmmo && this._activeFireArrowGO)
			{
				this.CurrentArrowItemView.ActiveBonus = (WeaponStatUpgrade.Types)(-1);
				global::UnityEngine.Object.Destroy(this._activeFireArrowGO.gameObject);
				this._activeFireArrowGO = null;
				LocalPlayer.Inventory.IsWeaponBurning = false;
				LocalPlayer.ScriptSetup.targetInfo.arrowFire = false;
			}
			this.BowItemView.ActiveBonus = bonusType;
		}

		// Token: 0x060057BD RID: 22461 RVA: 0x002A47A4 File Offset: 0x002A2BA4
		private void SetActiveArrowBonus(WeaponStatUpgrade.Types bonusType)
		{
			if (this._activeAmmoBonus != bonusType)
			{
				this._activeAmmoBonus = bonusType;
				this.UpdateArrowRenderer();
			}
			if (this._activeFireArrowGO)
			{
				global::UnityEngine.Object.Destroy(this._activeFireArrowGO.gameObject);
				this._activeFireArrowGO = null;
				LocalPlayer.Inventory.IsWeaponBurning = false;
				LocalPlayer.ScriptSetup.targetInfo.arrowFire = true;
			}
		}

		// Token: 0x060057BE RID: 22462 RVA: 0x002A480C File Offset: 0x002A2C0C
		private void UpdateArrowRenderer()
		{
			this.EnsureArrowIsInHierchy();
			if (this.CurrentArrowItemView.Properties.ActiveBonus == WeaponStatUpgrade.Types.BoneAmmo)
			{
				this._ammoAnimationRenderer.enabled = false;
				this._boneAmmoAnimationRenderer.enabled = true;
				this._modernAmmoAnimationRenderer.enabled = false;
			}
			else if (this.CurrentArrowItemView.Properties.ActiveBonus == WeaponStatUpgrade.Types.ModernAmmo)
			{
				this._ammoAnimationRenderer.enabled = false;
				this._boneAmmoAnimationRenderer.enabled = false;
				this._modernAmmoAnimationRenderer.enabled = true;
			}
			else
			{
				this._ammoAnimationRenderer.enabled = true;
				this._boneAmmoAnimationRenderer.enabled = false;
				this._modernAmmoAnimationRenderer.enabled = false;
				this._ammoAnimationRenderer.sharedMaterials = this.CurrentArrowItemView.GetComponent<Renderer>().sharedMaterials;
			}
		}

		// Token: 0x060057BF RID: 22463 RVA: 0x002A48E0 File Offset: 0x002A2CE0
		private WeaponStatUpgrade.Types NextAvailableArrowBonus(WeaponStatUpgrade.Types current)
		{
			if (!LocalPlayer.Inventory.Owns(this._ammoItemId, true))
			{
				return (WeaponStatUpgrade.Types)(-1);
			}
			WeaponStatUpgrade.Types types;
			switch (current)
			{
			case WeaponStatUpgrade.Types.BoneAmmo:
				types = WeaponStatUpgrade.Types.ModernAmmo;
				break;
			default:
				if (current != WeaponStatUpgrade.Types.BurningAmmo)
				{
					if (current != WeaponStatUpgrade.Types.PoisonnedAmmo)
					{
						types = WeaponStatUpgrade.Types.BurningAmmo;
					}
					else
					{
						types = WeaponStatUpgrade.Types.BoneAmmo;
					}
				}
				else
				{
					types = WeaponStatUpgrade.Types.PoisonnedAmmo;
				}
				break;
			case WeaponStatUpgrade.Types.ModernAmmo:
				types = (WeaponStatUpgrade.Types)(-1);
				break;
			}
			if (LocalPlayer.Inventory.OwnsItemWithBonus(this._ammoItemId, types))
			{
				return types;
			}
			return this.NextAvailableArrowBonus(types);
		}

		// Token: 0x060057C0 RID: 22464 RVA: 0x002A4979 File Offset: 0x002A2D79
		public bool IsUnlit()
		{
			return this.CanSetArrowOnFire && this._ammoAnimated.activeInHierarchy && !(this._aimingReticle != null);
		}

		// Token: 0x060057C1 RID: 22465 RVA: 0x002A49AE File Offset: 0x002A2DAE
		private void SetArrowOnFire()
		{
			this.ReArm();
			this._player.SpecialItems.SendMessage("LightHeldFire");
			base.CancelInvoke("LightArrow");
			base.Invoke("LightArrow", 2f);
			this._lightingArrow = true;
		}

		// Token: 0x060057C2 RID: 22466 RVA: 0x002A49F0 File Offset: 0x002A2DF0
		private void OnAmmoFired(GameObject Ammo)
		{
			GameStats.ArrowFired.Invoke();
			WeaponStatUpgrade.Types activeAmmoBonus = this._activeAmmoBonus;
			if (activeAmmoBonus != WeaponStatUpgrade.Types.BurningAmmo)
			{
				if (activeAmmoBonus == WeaponStatUpgrade.Types.PoisonnedAmmo)
				{
					GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this._poisonArrowPrefab);
					gameObject.transform.parent = Ammo.transform;
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;
				}
			}
			else if (this._activeFireArrowGO)
			{
				this._activeFireArrowGO.transform.parent = Ammo.transform;
				if (!this._activeFireArrowGO.GetComponent<destroyAfter>())
				{
					this._activeFireArrowGO.gameObject.AddComponent<destroyAfter>().destroyTime = 15f;
				}
				this._activeFireArrowGO = null;
				LocalPlayer.Inventory.IsWeaponBurning = false;
				LocalPlayer.ScriptSetup.targetInfo.arrowFire = false;
			}
			this.SetActiveArrowBonus((WeaponStatUpgrade.Types)(-1));
			if (this._currentAmmo != null)
			{
				this._currentAmmo.ActiveBonus = (WeaponStatUpgrade.Types)(-1);
			}
		}

		// Token: 0x060057C3 RID: 22467 RVA: 0x002A4B08 File Offset: 0x002A2F08
		private void setupBowForVr()
		{
			Hand componentInParent = base.transform.GetComponentInParent<Hand>();
			this._bowVr.SendMessage("SpawnAndAttachObject", componentInParent);
		}

		// Token: 0x170008E6 RID: 2278
		// (get) Token: 0x060057C4 RID: 22468 RVA: 0x002A4B34 File Offset: 0x002A2F34
		public bool CanSetArrowOnFire
		{
			get
			{
				return !this._lightingArrow && this._activeAmmoBonus == (WeaponStatUpgrade.Types)(-1) && !LighterControler.IsBusy && !LocalPlayer.Animator.GetBool("drawBowBool") && this._player.Owns(this._ammoItemId, false) && this.CurrentArrowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo;
			}
		}

		// Token: 0x170008E7 RID: 2279
		// (get) Token: 0x060057C5 RID: 22469 RVA: 0x002A4B9F File Offset: 0x002A2F9F
		private InventoryItemView BowItemView
		{
			get
			{
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._bowItemId][0];
			}
		}

		// Token: 0x170008E8 RID: 2280
		// (get) Token: 0x060057C6 RID: 22470 RVA: 0x002A4BBC File Offset: 0x002A2FBC
		private InventoryItemView CurrentArrowItemView
		{
			get
			{
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._ammoItemId][Mathf.Max(LocalPlayer.Inventory.AmountOf(this._ammoItemId, false) - 1, 0)];
			}
		}

		// Token: 0x170008E9 RID: 2281
		// (get) Token: 0x060057C7 RID: 22471 RVA: 0x002A4BF1 File Offset: 0x002A2FF1
		private InventoryItemView PrevioustArrowItemView
		{
			get
			{
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._ammoItemId][Mathf.Max(LocalPlayer.Inventory.AmountOf(this._ammoItemId, false) - 1, 0)];
			}
		}

		// Token: 0x04005D31 RID: 23857
		public PlayerInventory _player;

		// Token: 0x04005D32 RID: 23858
		[ItemIdPicker(Item.Types.RangedWeapon)]
		public int _bowItemId;

		// Token: 0x04005D33 RID: 23859
		[ItemIdPicker(Item.Types.Ammo)]
		public int _ammoItemId;

		// Token: 0x04005D34 RID: 23860
		public Transform _ammoHook;

		// Token: 0x04005D35 RID: 23861
		public GameObject _ammoAnimated;

		// Token: 0x04005D36 RID: 23862
		public Renderer _ammoAnimationRenderer;

		// Token: 0x04005D37 RID: 23863
		public Renderer _boneAmmoAnimationRenderer;

		// Token: 0x04005D38 RID: 23864
		public Renderer _modernAmmoAnimationRenderer;

		// Token: 0x04005D39 RID: 23865
		public float _reArmDelay = 0.5f;

		// Token: 0x04005D3A RID: 23866
		public float _bowSpeed;

		// Token: 0x04005D3B RID: 23867
		public MasterFireSpread _fireArrowPrefab;

		// Token: 0x04005D3C RID: 23868
		public GameObject _poisonArrowPrefab;

		// Token: 0x04005D3D RID: 23869
		public AimingReticle _aimingReticle;

		// Token: 0x04005D3E RID: 23870
		public GameObject _bowVr;

		// Token: 0x04005D3F RID: 23871
		public Longbow _longbowVr;

		// Token: 0x04005D40 RID: 23872
		public ArrowHand _arrowHandVr;

		// Token: 0x04005D41 RID: 23873
		private Animator _bowAnimatorVr;

		// Token: 0x04005D42 RID: 23874
		private InventoryItemView _currentAmmo;

		// Token: 0x04005D43 RID: 23875
		private bool _showRotateArrowType;

		// Token: 0x04005D44 RID: 23876
		private bool _lightingArrow;

		// Token: 0x04005D45 RID: 23877
		private int _attackHash;

		// Token: 0x04005D46 RID: 23878
		private int _drawIdleHash = Animator.StringToHash("drawBowIdle");

		// Token: 0x04005D47 RID: 23879
		private int _drawBowHash = Animator.StringToHash("drawBow");

		// Token: 0x04005D48 RID: 23880
		private int _releaseBow0Hash = Animator.StringToHash("releaseBow 0");

		// Token: 0x04005D49 RID: 23881
		private int _releaseBowHash = Animator.StringToHash("releaseBow");

		// Token: 0x04005D4A RID: 23882
		private int _bowIdleHash = Animator.StringToHash("bowIdle");

		// Token: 0x04005D4B RID: 23883
		private int _lightBowHash = Animator.StringToHash("lightBow");

		// Token: 0x04005D4C RID: 23884
		private int _bowPullVrHash = Animator.StringToHash("bowPull_VR");

		// Token: 0x04005D4D RID: 23885
		private Animator _animator;

		// Token: 0x04005D4E RID: 23886
		private Animator _bowAnimator;

		// Token: 0x04005D4F RID: 23887
		private float _nextReArm;

		// Token: 0x04005D50 RID: 23888
		public WeaponStatUpgrade.Types _activeAmmoBonus = (WeaponStatUpgrade.Types)(-1);

		// Token: 0x04005D51 RID: 23889
		public MasterFireSpread _activeFireArrowGO;

		// Token: 0x04005D52 RID: 23890
		private float _prevHoverRadius;
	}
}
