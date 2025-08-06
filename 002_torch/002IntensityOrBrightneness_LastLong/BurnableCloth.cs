using System;
using TheForest.Items.Craft;
using TheForest.Items.Inventory;
using TheForest.Items.Special;
using TheForest.Items.World.Interfaces;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TheForest.Items.World
{
	// Token: 0x02000CD7 RID: 3287
	[DoNotSerializePublic]
	public class BurnableCloth : MonoBehaviour, IBurnableItem
	{
		// Token: 0x170008EA RID: 2282
		// (get) Token: 0x060057C9 RID: 22473
		// (set) Token: 0x060057CA RID: 22474
		public BurnableCloth.States _state { get; private set; }

		// Token: 0x060057CB RID: 22475
		private void Awake()
		{
			this._normalMat = base.GetComponent<Renderer>().sharedMaterial;
			base.GetComponent<Renderer>().enabled = false;
			base.enabled = false;
			if (this._weaponFireSpawn != null)
			{
				this._weaponFireSpawn.transform.parent = base.transform.parent;
			}
		}

		// Token: 0x060057CC RID: 22476
		private void OnEnable()
		{
			if (this._state == BurnableCloth.States.Idle || this._state == BurnableCloth.States.PutOutIdle)
			{
				this._attacking = false;
				LighterControler.HasLightableItem = true;
				LocalPlayer.Inventory.Attacked.AddListener(new UnityAction(this.OnAttacking));
				LocalPlayer.Inventory.AttackEnded.AddListener(new UnityAction(this.OnAttackEnded));
				LocalPlayer.ActiveBurnableItem = this;
				this._onActivated.Invoke();
				if (ForestVR.Enabled)
				{
					LocalPlayer.vrPlayerControl.VRLightableTrigger.SetActive(true);
				}
			}
		}

		// Token: 0x060057CD RID: 22477
		private void OnDisable()
		{
			if (Scene.HudGui)
			{
				this._attacking = false;
				LocalPlayer.Inventory.Attacked.RemoveListener(new UnityAction(this.OnAttacking));
				LocalPlayer.Inventory.AttackEnded.RemoveListener(new UnityAction(this.OnAttackEnded));
				LighterControler.HasLightableItem = false;
				this.GotCleanDisable();
				if (this._state == BurnableCloth.States.Lighting)
				{
					this._state = BurnableCloth.States.Idle;
				}
				else if (this._state > BurnableCloth.States.Idle)
				{
					this.Burnt();
					this.Dissolved();
				}
				this._onDeactivated.Invoke();
			}
			if (this.Equals(LocalPlayer.ActiveBurnableItem))
			{
				LocalPlayer.ActiveBurnableItem = null;
			}
			if (ForestVR.Enabled)
			{
				LocalPlayer.vrPlayerControl.VRLightableTrigger.SetActive(false);
			}
		}

		// Token: 0x060057CE RID: 22478
		private void Update()
		{
			switch (this._state)
			{
			case BurnableCloth.States.PutOutIdle:
			case BurnableCloth.States.Idle:
				this.Idle();
				return;
			case BurnableCloth.States.PutOutLighting:
			case BurnableCloth.States.Lighting:
				this.Light();
				return;
			case BurnableCloth.States.Burning:
				this.Burning();
				return;
			case BurnableCloth.States.Burnt:
				this.Burnt();
				return;
			case BurnableCloth.States.Dissolving:
				this.Dissolving();
				return;
			case BurnableCloth.States.Dissolved:
				this.Dissolved();
				return;
			default:
				return;
			}
		}

		// Token: 0x060057CF RID: 22479
		public void OnDeserialized()
		{
			InventoryItemView inventoryItemView = this._inventoryMirror.transform.parent.parent.GetComponent<InventoryItemView>() ?? this._inventoryMirror.transform.parent.GetComponent<InventoryItemView>();
			if (inventoryItemView)
			{
				WeaponStatUpgrade.Types activeBonus = inventoryItemView.ActiveBonus;
				if (activeBonus == WeaponStatUpgrade.Types.BurningWeapon || activeBonus == WeaponStatUpgrade.Types.BurningWeaponExtra)
				{
					this.EnableBurnableCloth();
				}
			}
		}

		// Token: 0x060057D0 RID: 22480
		public void EnableBurnableCloth()
		{
			if (this._inventoryMirror)
			{
				this._inventoryMirror.SetActive(true);
			}
			if (this._craftMirror)
			{
				this._craftMirror.SetActive(true);
			}
			this._state = BurnableCloth.States.Idle;
			base.GetComponent<Renderer>().enabled = true;
			base.enabled = true;
		}

		// Token: 0x060057D1 RID: 22481
		public void EnableBurnableClothExtra()
		{
		}

		// Token: 0x060057D2 RID: 22482
		public void GotClean()
		{
		}

		// Token: 0x060057D3 RID: 22483
		private void GotCleanDisable()
		{
			if (this._state == BurnableCloth.States.Burning)
			{
				this._putOutFuel = this._fuel;
				this.Burnt();
				this._clothDisolveMat.SetFloat("_BurnAmount", 0f);
				this._state = BurnableCloth.States.PutOutIdle;
				this._player.IsWeaponBurning = false;
				LighterControler.HasLightableItem = true;
				FMODCommon.PlayOneshotNetworked("event:/player/actions/molotov_quench", base.transform, FMODCommon.NetworkRole.Any);
			}
		}

		// Token: 0x060057D4 RID: 22484
		private void OnAttacking()
		{
			this._attacking = true;
		}

		// Token: 0x060057D5 RID: 22485
		private void OnAttackEnded()
		{
			this._attacking = false;
		}

		// Token: 0x060057D6 RID: 22486
		private void Idle()
		{
			if (TheForest.Utils.Input.GetButtonAfterDelay("Lighter", 0.5f, false))
			{
				if (!LocalPlayer.Inventory.DefaultLight.IsReallyActive)
				{
					LocalPlayer.Inventory.LastLight = LocalPlayer.Inventory.DefaultLight;
					LocalPlayer.Inventory.TurnOnLastLight();
				}
				LocalPlayer.Inventory.SpecialItems.SendMessage("LightHeldFire");
				this._fuel = Time.time + this._lightingDuration;
				this._state = ((this._state != BurnableCloth.States.PutOutIdle) ? BurnableCloth.States.Lighting : BurnableCloth.States.PutOutLighting);
				LighterControler.HasLightableItem = false;
			}
		}

		// Token: 0x060057D7 RID: 22487
		private void Light()
		{
			if (this._fuel < Time.time)
			{
				GameStats.LitWeapon.Invoke();
				LocalPlayer.Inventory.DefaultLight.StashLighter();
				object obj = this._inventoryMirror.transform.parent.parent.GetComponent<InventoryItemView>() ?? this._inventoryMirror.transform.parent.GetComponent<InventoryItemView>();
				Transform transform = ((!this._weaponFireSpawn) ? base.transform : this._weaponFireSpawn.transform);
				this._weaponFire = global::UnityEngine.Object.Instantiate<GameObject>(this._weaponFirePrefab, transform.position, transform.rotation);
				this._weaponFire.transform.parent = transform;
				if (!this._weaponFire.activeSelf)
				{
					this._weaponFire.gameObject.SetActive(true);
				}
				if (this._customFireEffect)
				{
					this._customFireEffect.SetActive(true);
					weaponFireParticleController componentInChildren = this._weaponFire.GetComponentInChildren<weaponFireParticleController>();
					if (componentInChildren)
					{
						global::UnityEngine.Object.Destroy(componentInChildren.gameObject);
					}
				}
				this._fireParticleScale = this._weaponFire.GetComponentInChildren<ParticleScaler>();
				this._firelight = this._weaponFire.GetComponentInChildren<Light>();
				this._fireAudioEmitter = this._weaponFire.GetComponent<FMOD_StudioEventEmitter>();
				base.GetComponent<Renderer>().sharedMaterial = this._burningMat;
				this._fuel = ((this._state != BurnableCloth.States.PutOutLighting) ? this._burnDuration : this._putOutFuel);
				object obj2 = obj;
				if (obj2.ActiveBonus == WeaponStatUpgrade.Types.BurningWeaponExtra)
				{
					this._extraBurn = true;
					this._fuel *= 3f;
				}
				else
				{
					this._extraBurn = false;
				}
				this._state = BurnableCloth.States.Burning;
				this._player.IsWeaponBurning = true;
				this._attacking = false;
				obj2.ActiveBonus = (WeaponStatUpgrade.Types)(-1);
				FMODCommon.PlayOneshot("event:/fire/fire_built_start", transform);
			}
		}

		// Token: 0x060057D8 RID: 22488
		private void Burning()
		{
			if (this._fuel < 1.5f && this._weaponFire)
			{
				this._weaponFire.SendMessage("setFireTimeout", SendMessageOptions.DontRequireReceiver);
			}
			if (this._fuel >= 0f)
			{
				if (Scene.WeatherSystem.Raining)
				{
					this._fuel -= Time.deltaTime * ((!this._attacking) ? 5f : (this._fuelRatioAttacking + 5f)) * 0.5f;
				}
				else
				{
					this._fuel -= Time.deltaTime * ((!this._attacking) ? 1f : this._fuelRatioAttacking) * 0.5f;
				}
				float num = this.ExpoEaseIn(1f - this._fuel / this._burnDuration, 1f, 0f, 1f) * this._fireParticleSize * ((!this._attacking) ? 1f : 0.75f);
				if (this._extraBurn)
				{
					num *= 1.2f;
				}
				if (this._fireParticleScale)
				{
					this._fireParticleScale.particleScale = num;
				}
				if (this._firelight)
				{
					this._firelight.intensity = num * 0.42857143f * this._firelightIntensityRatio * ((!this._attacking) ? 2f : 1.8f);
				}
				if (this._fireAudioEmitter)
				{
					this._fireAudioEmitter.SetVolume(num);
					return;
				}
			}
			else
			{
				this._state = BurnableCloth.States.Burnt;
			}
		}

		// Token: 0x060057D9 RID: 22489
		private void Burnt()
		{
			if (this._weaponFire)
			{
				global::UnityEngine.Object.Destroy(this._weaponFire);
				this._weaponFire = null;
			}
			if (this._customFireEffect)
			{
				this._customFireEffect.SetActive(false);
			}
			base.GetComponent<Renderer>().sharedMaterial = this._clothDisolveMat;
			this._clothDisolveMat.SetFloat("_BurnAmount", 1f);
			this._fireParticleScale = null;
			this._firelight = null;
			this._fuel = this._dissolveDuration;
			this._state = BurnableCloth.States.Dissolving;
		}

		// Token: 0x060057DA RID: 22490
		private void Dissolving()
		{
			this._fuel -= Time.deltaTime;
			if (this._fuel > 0f)
			{
				this._clothDisolveMat.SetFloat("_BurnAmount", 1f - this._fuel / this._dissolveDuration);
				return;
			}
			this._state = BurnableCloth.States.Dissolved;
		}

		// Token: 0x060057DB RID: 22491
		private void Dissolved()
		{
			if (this._inventoryMirror)
			{
				this._inventoryMirror.SetActive(false);
			}
			if (this._craftMirror)
			{
				this._craftMirror.SetActive(false);
			}
			base.GetComponent<Renderer>().enabled = false;
			base.GetComponent<Renderer>().sharedMaterial = this._normalMat;
			this._clothDisolveMat.SetFloat("_BurnAmount", 1f);
			this._state = BurnableCloth.States.Disabled;
			this._player.IsWeaponBurning = false;
			base.enabled = false;
		}

		// Token: 0x060057DC RID: 22492
		public float ExpoEaseIn(float t, float b, float c, float d)
		{
			if (t == 0f)
			{
				return b;
			}
			return c * Mathf.Pow(2f, 10f * (t / d - 1f)) + b;
		}

		// Token: 0x060057DD RID: 22493
		public bool IsUnlit()
		{
			return this._state == BurnableCloth.States.Idle || this._state == BurnableCloth.States.PutOutIdle;
		}

		// Token: 0x170008EB RID: 2283
		// (get) Token: 0x060057DE RID: 22494
		public BurnableCloth.States State
		{
			get
			{
				return this._state;
			}
		}

		// Token: 0x04005D53 RID: 23891
		public PlayerInventory _player;

		// Token: 0x04005D54 RID: 23892
		public float _lightingDuration = 1.5f;

		// Token: 0x04005D55 RID: 23893
		public float _burnDuration = 60f;

		// Token: 0x04005D56 RID: 23894
		public float _fuelRatioAttacking = 20f;

		// Token: 0x04005D57 RID: 23895
		public float _dissolveDuration = 1.5f;

		// Token: 0x04005D58 RID: 23896
		public float _fireParticleSize = 1.2f;

		// Token: 0x04005D59 RID: 23897
		public float _firelightIntensityRatio = 1f;

		// Token: 0x04005D5A RID: 23898
		public GameObject _customFireEffect;

		// Token: 0x04005D5B RID: 23899
		public Material _burningMat;

		// Token: 0x04005D5C RID: 23900
		public GameObject _weaponFirePrefab;

		// Token: 0x04005D5D RID: 23901
		public Material _clothDisolveMat;

		// Token: 0x04005D5E RID: 23902
		public GameObject _weaponFireSpawn;

		// Token: 0x04005D5F RID: 23903
		public GameObject _inventoryMirror;

		// Token: 0x04005D60 RID: 23904
		public GameObject _craftMirror;

		// Token: 0x04005D61 RID: 23905
		public UnityEvent _onActivated;

		// Token: 0x04005D62 RID: 23906
		public UnityEvent _onDeactivated;

		// Token: 0x04005D64 RID: 23908
		private bool _extraBurn;

		// Token: 0x04005D65 RID: 23909
		private bool _attacking;

		// Token: 0x04005D66 RID: 23910
		private float _putOutFuel;

		// Token: 0x04005D67 RID: 23911
		private float _fuel;

		// Token: 0x04005D68 RID: 23912
		private Material _normalMat;

		// Token: 0x04005D69 RID: 23913
		private GameObject _weaponFire;

		// Token: 0x04005D6A RID: 23914
		private ParticleScaler _fireParticleScale;

		// Token: 0x04005D6B RID: 23915
		private Light _firelight;

		// Token: 0x04005D6C RID: 23916
		private FMOD_StudioEventEmitter _fireAudioEmitter;

		// Token: 0x02000CD8 RID: 3288
		public enum States
		{
			// Token: 0x04005D6E RID: 23918
			Disabled,
			// Token: 0x04005D6F RID: 23919
			PutOutIdle,
			// Token: 0x04005D70 RID: 23920
			Idle,
			// Token: 0x04005D71 RID: 23921
			PutOutLighting,
			// Token: 0x04005D72 RID: 23922
			Lighting,
			// Token: 0x04005D73 RID: 23923
			Burning,
			// Token: 0x04005D74 RID: 23924
			Burnt,
			// Token: 0x04005D75 RID: 23925
			Dissolving,
			// Token: 0x04005D76 RID: 23926
			Dissolved
		}
	}
}
