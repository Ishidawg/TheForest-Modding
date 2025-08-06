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
	// Token: 0x02000FD1 RID: 4049
	public class BowController : MonoBehaviour, IBurnableItem
	{
		// Token: 0x06006832 RID: 26674
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

		// Token: 0x06006833 RID: 26675
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

		// Token: 0x06006834 RID: 26676
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

		// Token: 0x06006835 RID: 26677
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
				if (this.CanSetArrowOnFire)
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
							return;
						}
						if (LocalPlayer.Animator.GetBool("ballHeld"))
						{
							LocalPlayer.AnimControl.animEvents.enableSpine();
							this._player.CancelNextChargedAttack = true;
							this._animator.SetBoolReflected("drawBowBool", false);
							this._bowAnimator.SetBoolReflected("drawBool", false);
							this.ShutDown(false);
							return;
						}
						if ((LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBow0Hash || LocalPlayer.AnimControl.currLayerState0.shortNameHash == LocalPlayer.AnimControl.landHeavyHash || LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._releaseBowHash || LocalPlayer.AnimControl.currLayerState1.shortNameHash == this._drawBowHash || LocalPlayer.AnimControl.nextLayerState1.shortNameHash == this._drawBowHash) && LocalPlayer.AnimControl.nextLayerState1.shortNameHash != this._bowIdleHash)
						{
							if (LocalPlayer.Inventory.blockRangedAttack)
							{
								this.ShutDown(false);
								return;
							}
							this.ShutDown(true);
							return;
						}
					}
					else if (this._nextReArm < Time.time)
					{
						this.ReArm();
						return;
					}
				}
				else if (!ForestVR.Enabled)
				{
					LocalPlayer.Inventory.CancelNextChargedAttack = true;
				}
			}
		}

		// Token: 0x06006836 RID: 26678
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
						return;
					}
					this._ammoAnimated.SetActive(false);
				}
			}
		}

		// Token: 0x06006837 RID: 26679
		private void OnDestroy()
		{
			if (this._ammoAnimated)
			{
				global::UnityEngine.Object.Destroy(this._ammoAnimated);
			}
		}

		// Token: 0x06006838 RID: 26680
		private void OnItemAdded(object o)
		{
			this.OnItemAdded((int)o);
		}

		// Token: 0x06006839 RID: 26681
		private void OnItemAdded(int itemId)
		{
			if (this._ammoItemId == itemId && this._player.AmountOf(this._ammoItemId, false) == 1)
			{
				this._player.ToggleAmmo(this._ammoItemId, true);
			}
		}

		// Token: 0x0600683A RID: 26682
		private void ShutDownFire()
		{
			this.SetActiveArrowBonus((WeaponStatUpgrade.Types)(-1));
			LocalPlayer.Inventory.IsWeaponBurning = false;
			LocalPlayer.ScriptSetup.targetInfo.arrowFire = false;
		}

		// Token: 0x0600683B RID: 26683
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

		// Token: 0x0600683C RID: 26684
		private void InitReArm()
		{
			if (this._activeFireArrowGO)
			{
				this._activeFireArrowGO.transform.parent = null;
			}
			this._ammoAnimated.SetActive(false);
			this._nextReArm = Time.time + this._reArmDelay;
		}

		// Token: 0x0600683D RID: 26685
		private void ReArm()
		{
			this._nextReArm = float.MaxValue;
			this.EnsureArrowIsInHierchy();
			this._ammoAnimated.SetActive(true);
		}

		// Token: 0x0600683E RID: 26686
		private void EnsureArrowIsInHierchy()
		{
			if (!Reparent.Locked && this._ammoAnimationRenderer.transform.parent.parent != this._ammoHook && base.transform.root.CompareTag("Player"))
			{
				this._ammoAnimationRenderer.transform.parent.parent = this._ammoHook;
				this._ammoAnimationRenderer.transform.parent.localPosition = Vector3.zero;
				this._ammoAnimationRenderer.transform.parent.localRotation = Quaternion.identity;
			}
		}

		// Token: 0x0600683F RID: 26687
		private void LightArrowVR()
		{
			if (this.CanSetArrowOnFire)
			{
				this.LightArrow();
			}
		}

		// Token: 0x06006840 RID: 26688
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

		// Token: 0x06006841 RID: 26689
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

		// Token: 0x06006842 RID: 26690
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

		// Token: 0x06006843 RID: 26691
		private void UpdateArrowRenderer()
		{
			this.EnsureArrowIsInHierchy();
			if (this.CurrentArrowItemView.Properties.ActiveBonus == WeaponStatUpgrade.Types.BoneAmmo)
			{
				this._ammoAnimationRenderer.enabled = false;
				this._boneAmmoAnimationRenderer.enabled = true;
				this._modernAmmoAnimationRenderer.enabled = false;
				return;
			}
			if (this.CurrentArrowItemView.Properties.ActiveBonus == WeaponStatUpgrade.Types.ModernAmmo)
			{
				this._ammoAnimationRenderer.enabled = false;
				this._boneAmmoAnimationRenderer.enabled = false;
				this._modernAmmoAnimationRenderer.enabled = true;
				return;
			}
			this._ammoAnimationRenderer.enabled = true;
			this._boneAmmoAnimationRenderer.enabled = false;
			this._modernAmmoAnimationRenderer.enabled = false;
			this._ammoAnimationRenderer.sharedMaterials = this.CurrentArrowItemView.GetComponent<Renderer>().sharedMaterials;
		}

		// Token: 0x06006844 RID: 26692
		private WeaponStatUpgrade.Types NextAvailableArrowBonus(WeaponStatUpgrade.Types current)
		{
			if (!LocalPlayer.Inventory.Owns(this._ammoItemId, true))
			{
				return (WeaponStatUpgrade.Types)(-1);
			}
			WeaponStatUpgrade.Types types;
			if (current != WeaponStatUpgrade.Types.BoneAmmo)
			{
				if (current != WeaponStatUpgrade.Types.ModernAmmo)
				{
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
				}
				else
				{
					types = (WeaponStatUpgrade.Types)(-1);
				}
			}
			else
			{
				types = WeaponStatUpgrade.Types.ModernAmmo;
			}
			if (LocalPlayer.Inventory.OwnsItemWithBonus(this._ammoItemId, types))
			{
				return types;
			}
			return this.NextAvailableArrowBonus(types);
		}

		// Token: 0x06006845 RID: 26693
		public bool IsUnlit()
		{
			return this.CanSetArrowOnFire && this._ammoAnimated.activeInHierarchy && !(this._aimingReticle != null);
		}

		// Token: 0x06006846 RID: 26694
		private void SetArrowOnFire()
		{
			this.ReArm();
			this._player.SpecialItems.SendMessage("LightHeldFire");
			base.CancelInvoke("LightArrow");
			base.Invoke("LightArrow", 2f);
			this._lightingArrow = true;
		}

		// Token: 0x06006847 RID: 26695
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

		// Token: 0x06006848 RID: 26696
		private void setupBowForVr()
		{
			Hand componentInParent = base.transform.GetComponentInParent<Hand>();
			this._bowVr.SendMessage("SpawnAndAttachObject", componentInParent);
		}

		// Token: 0x17000DF2 RID: 3570
		// (get) Token: 0x06006849 RID: 26697
		public bool CanSetArrowOnFire
		{
			get
			{
				return !this._lightingArrow && this._activeAmmoBonus == (WeaponStatUpgrade.Types)(-1) && !LighterControler.IsBusy && !LocalPlayer.Animator.GetBool("drawBowBool") && this._player.Owns(this._ammoItemId, false) && this.CurrentArrowItemView.ActiveBonus == WeaponStatUpgrade.Types.BurningAmmo;
			}
		}

		// Token: 0x17000DF3 RID: 3571
		// (get) Token: 0x0600684A RID: 26698
		private InventoryItemView BowItemView
		{
			get
			{
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._bowItemId][0];
			}
		}

		// Token: 0x17000DF4 RID: 3572
		// (get) Token: 0x0600684B RID: 26699
		private InventoryItemView CurrentArrowItemView
		{
			get
			{
				int index = Mathf.Min(LocalPlayer.Inventory.AmountOf(this._ammoItemId, false), 50) - 1;
				if (index < 0)
				{
					index = 0;
				}
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._ammoItemId][index];
			}
		}

		// Token: 0x17000DF5 RID: 3573
		// (get) Token: 0x0600684C RID: 26700
		private InventoryItemView PrevioustArrowItemView
		{
			get
			{
				return LocalPlayer.Inventory.InventoryItemViewsCache[this._ammoItemId][Mathf.Max(LocalPlayer.Inventory.AmountOf(this._ammoItemId, false) - 1, 0)];
			}
		}

		// Token: 0x04006D9D RID: 28061
		public PlayerInventory _player;

		// Token: 0x04006D9E RID: 28062
		[ItemIdPicker(Item.Types.RangedWeapon)]
		public int _bowItemId;

		// Token: 0x04006D9F RID: 28063
		[ItemIdPicker(Item.Types.Ammo)]
		public int _ammoItemId;

		// Token: 0x04006DA0 RID: 28064
		public Transform _ammoHook;

		// Token: 0x04006DA1 RID: 28065
		public GameObject _ammoAnimated;

		// Token: 0x04006DA2 RID: 28066
		public Renderer _ammoAnimationRenderer;

		// Token: 0x04006DA3 RID: 28067
		public Renderer _boneAmmoAnimationRenderer;

		// Token: 0x04006DA4 RID: 28068
		public Renderer _modernAmmoAnimationRenderer;

		// Token: 0x04006DA5 RID: 28069
		public float _reArmDelay = 0.5f;

		// Token: 0x04006DA6 RID: 28070
		public float _bowSpeed;

		// Token: 0x04006DA7 RID: 28071
		public MasterFireSpread _fireArrowPrefab;

		// Token: 0x04006DA8 RID: 28072
		public GameObject _poisonArrowPrefab;

		// Token: 0x04006DA9 RID: 28073
		public AimingReticle _aimingReticle;

		// Token: 0x04006DAA RID: 28074
		public GameObject _bowVr;

		// Token: 0x04006DAB RID: 28075
		public Longbow _longbowVr;

		// Token: 0x04006DAC RID: 28076
		public ArrowHand _arrowHandVr;

		// Token: 0x04006DAD RID: 28077
		private Animator _bowAnimatorVr;

		// Token: 0x04006DAE RID: 28078
		private InventoryItemView _currentAmmo;

		// Token: 0x04006DAF RID: 28079
		private bool _showRotateArrowType;

		// Token: 0x04006DB0 RID: 28080
		private bool _lightingArrow;

		// Token: 0x04006DB1 RID: 28081
		private int _attackHash;

		// Token: 0x04006DB2 RID: 28082
		private int _drawIdleHash = Animator.StringToHash("drawBowIdle");

		// Token: 0x04006DB3 RID: 28083
		private int _drawBowHash = Animator.StringToHash("drawBow");

		// Token: 0x04006DB4 RID: 28084
		private int _releaseBow0Hash = Animator.StringToHash("releaseBow 0");

		// Token: 0x04006DB5 RID: 28085
		private int _releaseBowHash = Animator.StringToHash("releaseBow");

		// Token: 0x04006DB6 RID: 28086
		private int _bowIdleHash = Animator.StringToHash("bowIdle");

		// Token: 0x04006DB7 RID: 28087
		private int _lightBowHash = Animator.StringToHash("lightBow");

		// Token: 0x04006DB8 RID: 28088
		private int _bowPullVrHash = Animator.StringToHash("bowPull_VR");

		// Token: 0x04006DB9 RID: 28089
		private Animator _animator;

		// Token: 0x04006DBA RID: 28090
		private Animator _bowAnimator;

		// Token: 0x04006DBB RID: 28091
		private float _nextReArm;

		// Token: 0x04006DBC RID: 28092
		public WeaponStatUpgrade.Types _activeAmmoBonus = (WeaponStatUpgrade.Types)(-1);

		// Token: 0x04006DBD RID: 28093
		public MasterFireSpread _activeFireArrowGO;

		// Token: 0x04006DBE RID: 28094
		private float _prevHoverRadius;
	}
}
