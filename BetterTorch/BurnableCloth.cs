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
	// Token: 0x02000FD2 RID: 4050
	[DoNotSerializePublic]
	public class BurnableCloth : MonoBehaviour, IBurnableItem
	{
		// Token: 0x17000DF6 RID: 3574
		// (get) Token: 0x0600684E RID: 26702 RVA: 0x000425F4 File Offset: 0x000407F4
		// (set) Token: 0x0600684F RID: 26703 RVA: 0x000425FC File Offset: 0x000407FC
		public BurnableCloth.States _state { get; private set; }

		// Token: 0x06006850 RID: 26704 RVA: 0x002B6A08 File Offset: 0x002B4C08
		private void Awake()
		{
			this._normalMat = base.GetComponent<Renderer>().sharedMaterial;
			base.GetComponent<Renderer>().enabled = false;
			base.enabled = false;
			if (this._weaponFireSpawn != null)
			{
				this._weaponFireSpawn.transform.parent = base.transform.parent;
			}
			this._lightingDuration = 1.4f;
			this._burnDuration = 120f;
			this._firelightIntensityRatio = 3.2f;
		}

		// Token: 0x06006851 RID: 26705 RVA: 0x002B6A84 File Offset: 0x002B4C84
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

		// Token: 0x06006852 RID: 26706 RVA: 0x002B6B10 File Offset: 0x002B4D10
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

		// Token: 0x06006853 RID: 26707 RVA: 0x002B6BD0 File Offset: 0x002B4DD0
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

		// Token: 0x06006854 RID: 26708 RVA: 0x002B6C38 File Offset: 0x002B4E38
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

		// Token: 0x06006855 RID: 26709 RVA: 0x002B6C98 File Offset: 0x002B4E98
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

		// Token: 0x06006856 RID: 26710 RVA: 0x000037AA File Offset: 0x000019AA
		public void EnableBurnableClothExtra()
		{
		}

		// Token: 0x06006857 RID: 26711 RVA: 0x000037AA File Offset: 0x000019AA
		public void GotClean()
		{
		}

		// Token: 0x06006858 RID: 26712 RVA: 0x002B6CF4 File Offset: 0x002B4EF4
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

		// Token: 0x06006859 RID: 26713 RVA: 0x00042605 File Offset: 0x00040805
		private void OnAttacking()
		{
			this._attacking = true;
		}

		// Token: 0x0600685A RID: 26714 RVA: 0x0004260E File Offset: 0x0004080E
		private void OnAttackEnded()
		{
			this._attacking = false;
		}

		// Token: 0x0600685B RID: 26715 RVA: 0x002B6D5C File Offset: 0x002B4F5C
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

		// Token: 0x0600685C RID: 26716 RVA: 0x002B6DEC File Offset: 0x002B4FEC
		private void Light()
		{
			if (this._fuel < Time.time)
			{
				GameStats.LitWeapon.Invoke();
				LocalPlayer.Inventory.DefaultLight.StashLighter();
				object obj = this._inventoryMirror.transform.parent.parent.GetComponent<InventoryItemView>() ?? this._inventoryMirror.transform.parent.GetComponent<InventoryItemView>();
				Transform transform = (!this._weaponFireSpawn) ? base.transform : this._weaponFireSpawn.transform;
				this._weaponFire = UnityEngine.Object.Instantiate<GameObject>(this._weaponFirePrefab, transform.position, transform.rotation);
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
						UnityEngine.Object.Destroy(componentInChildren.gameObject);
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

		// Token: 0x0600685D RID: 26717 RVA: 0x002B6FB4 File Offset: 0x002B51B4
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
					this._fuel -= Time.deltaTime * ((!this._attacking) ? 5f : (this._fuelRatioAttacking + 5f));
				}
				else
				{
					this._fuel -= Time.deltaTime * ((!this._attacking) ? 1f : this._fuelRatioAttacking);
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
					this._firelight.intensity = num * 0.42857143f * this._firelightIntensityRatio * ((!this._attacking) ? 1f : 0.8f);
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

		// Token: 0x0600685E RID: 26718 RVA: 0x002B712C File Offset: 0x002B532C
		private void Burnt()
		{
			if (this._weaponFire)
			{
				UnityEngine.Object.Destroy(this._weaponFire);
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

		// Token: 0x0600685F RID: 26719 RVA: 0x002B71B8 File Offset: 0x002B53B8
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

		// Token: 0x06006860 RID: 26720 RVA: 0x002B7210 File Offset: 0x002B5410
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

		// Token: 0x06006861 RID: 26721 RVA: 0x00042617 File Offset: 0x00040817
		public float ExpoEaseIn(float t, float b, float c, float d)
		{
			if (t == 0f)
			{
				return b;
			}
			return c * Mathf.Pow(2f, 10f * (t / d - 1f)) + b;
		}

		// Token: 0x06006862 RID: 26722 RVA: 0x00042641 File Offset: 0x00040841
		public bool IsUnlit()
		{
			return this._state == BurnableCloth.States.Idle || this._state == BurnableCloth.States.PutOutIdle;
		}

		// Token: 0x17000DF7 RID: 3575
		// (get) Token: 0x06006863 RID: 26723 RVA: 0x00042657 File Offset: 0x00040857
		public BurnableCloth.States State
		{
			get
			{
				return this._state;
			}
		}

		// Token: 0x04006DBF RID: 28095
		public PlayerInventory _player;

		// Token: 0x04006DC0 RID: 28096
		public float _lightingDuration;

		// Token: 0x04006DC1 RID: 28097
		public float _burnDuration;

		// Token: 0x04006DC2 RID: 28098
		public float _fuelRatioAttacking = 20f;

		// Token: 0x04006DC3 RID: 28099
		public float _dissolveDuration = 1.5f;

		// Token: 0x04006DC4 RID: 28100
		public float _fireParticleSize = 1.2f;

		// Token: 0x04006DC5 RID: 28101
		public float _firelightIntensityRatio;

		// Token: 0x04006DC6 RID: 28102
		public GameObject _customFireEffect;

		// Token: 0x04006DC7 RID: 28103
		public Material _burningMat;

		// Token: 0x04006DC8 RID: 28104
		public GameObject _weaponFirePrefab;

		// Token: 0x04006DC9 RID: 28105
		public Material _clothDisolveMat;

		// Token: 0x04006DCA RID: 28106
		public GameObject _weaponFireSpawn;

		// Token: 0x04006DCB RID: 28107
		public GameObject _inventoryMirror;

		// Token: 0x04006DCC RID: 28108
		public GameObject _craftMirror;

		// Token: 0x04006DCD RID: 28109
		public UnityEvent _onActivated;

		// Token: 0x04006DCE RID: 28110
		public UnityEvent _onDeactivated;

		// Token: 0x04006DD0 RID: 28112
		private bool _extraBurn;

		// Token: 0x04006DD1 RID: 28113
		private bool _attacking;

		// Token: 0x04006DD2 RID: 28114
		private float _putOutFuel;

		// Token: 0x04006DD3 RID: 28115
		private float _fuel;

		// Token: 0x04006DD4 RID: 28116
		private Material _normalMat;

		// Token: 0x04006DD5 RID: 28117
		private GameObject _weaponFire;

		// Token: 0x04006DD6 RID: 28118
		private ParticleScaler _fireParticleScale;

		// Token: 0x04006DD7 RID: 28119
		private Light _firelight;

		// Token: 0x04006DD8 RID: 28120
		private FMOD_StudioEventEmitter _fireAudioEmitter;

		// Token: 0x02000FD3 RID: 4051
		public enum States
		{
			// Token: 0x04006DDA RID: 28122
			Disabled,
			// Token: 0x04006DDB RID: 28123
			PutOutIdle,
			// Token: 0x04006DDC RID: 28124
			Idle,
			// Token: 0x04006DDD RID: 28125
			PutOutLighting,
			// Token: 0x04006DDE RID: 28126
			Lighting,
			// Token: 0x04006DDF RID: 28127
			Burning,
			// Token: 0x04006DE0 RID: 28128
			Burnt,
			// Token: 0x04006DE1 RID: 28129
			Dissolving,
			// Token: 0x04006DE2 RID: 28130
			Dissolved
		}
	}
}
