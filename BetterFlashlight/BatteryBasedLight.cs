using System;
using System.Collections;
using Bolt;
using TheForest.Utils;
using UnityEngine;

namespace TheForest.Items.World
{
	// Token: 0x02000CD3 RID: 3283
	[AddComponentMenu("Items/World/Battery Based Light")]
	public class BatteryBasedLight : EntityBehaviour<IPlayerState>
	{
		// Token: 0x06005796 RID: 22422
		private void Awake()
		{
			this._vis = base.transform.root.GetComponent<netPlayerVis>();
			this.SetColor(this._torchBaseColor);
		}

		// Token: 0x06005797 RID: 22423
		private void OnEnable()
		{
			if (!BoltNetwork.isRunning || (BoltNetwork.isRunning && base.entity && base.entity.isAttached))
			{
				if (!BoltNetwork.isRunning || base.entity.isOwner)
				{
					base.StartCoroutine(this.DelayedLightOn());
					this._doingStash = false;
					return;
				}
				this.SetEnabled(base.state.BatteryTorchEnabled);
			}
		}

		// Token: 0x06005798 RID: 22424
		private void OnDisable()
		{
			this.SetEnabled(false);
			if (this._manageDynamicShadows)
			{
				if (CoopPeerStarter.DedicatedHost || LocalPlayer.Transform == null || Scene.SceneTracker == null)
				{
					return;
				}
				if (Scene.SceneTracker.activePlayerLights.Contains(base.gameObject))
				{
					Scene.SceneTracker.activePlayerLights.Remove(base.gameObject);
				}
			}
			if (LocalPlayer.Animator)
			{
				LocalPlayer.Animator.SetBool("noBattery", false);
			}
			this._animCoolDown = 0f;
			base.CancelInvoke("resetBatteryBool");
			this._doingStash = false;
			base.StopAllCoroutines();
		}

		// Token: 0x06005799 RID: 22425
		private void Update()
		{
			if (!BoltNetwork.isRunning || (BoltNetwork.isRunning && base.entity && base.entity.isAttached && base.entity.isOwner))
			{
				LocalPlayer.Stats.BatteryCharge -= this._batterieCostPerSecond * Time.deltaTime;
				if (LocalPlayer.Stats.BatteryCharge > 50f)
				{
					this.SetIntensity(this._highBatteryIntensity);
				}
				else if (LocalPlayer.Stats.BatteryCharge < 20f)
				{
					if (LocalPlayer.Stats.BatteryCharge < 10f)
					{
						if (LocalPlayer.Stats.BatteryCharge < 5f)
						{
							if (LocalPlayer.Stats.BatteryCharge < 3f && Time.time > this._animCoolDown && !this._skipNoBatteryRoutine)
							{
								LocalPlayer.Animator.SetBool("noBattery", true);
								this._animCoolDown = Time.time + (float)global::UnityEngine.Random.Range(30, 60);
								base.Invoke("resetBatteryBool", 1.5f);
							}
							if (LocalPlayer.Stats.BatteryCharge <= 0f)
							{
								LocalPlayer.Stats.BatteryCharge = 0f;
								if (this._skipNoBatteryRoutine)
								{
									this.SetEnabled(false);
								}
								else
								{
									this.TorchLowerLightEvenMore();
									if (!this._doingStash)
									{
										base.StartCoroutine("stashNoBatteryRoutine");
									}
									this._doingStash = true;
								}
							}
							else
							{
								this.SetEnabled(true);
							}
						}
						else
						{
							this.TorchLowerLightMore();
							this.SetEnabled(true);
						}
					}
					else
					{
						this.TorchLowerLight();
						this.SetEnabled(true);
					}
				}
				if (BoltNetwork.isRunning)
				{
					base.state.BatteryTorchIntensity = this._mainLight.intensity;
					base.state.BatteryTorchEnabled = this._mainLight.enabled;
					base.state.BatteryTorchColor = this._mainLight.color;
				}
			}
			if (BoltNetwork.isRunning && base.entity && base.entity.isAttached && !base.entity.isOwner)
			{
				this.SetEnabled(base.state.BatteryTorchEnabled);
			}
			if (ForestVR.Enabled)
			{
				TheForestQualitySettings.UserSettings.ApplyQualitySetting(this._mainLight, LightShadows.None);
				return;
			}
			if (this._manageDynamicShadows)
			{
				this.manageShadows();
				return;
			}
			TheForestQualitySettings.UserSettings.ApplyQualitySetting(this._mainLight, LightShadows.Hard);
		}

		// Token: 0x0600579A RID: 22426
		private IEnumerator DelayedLightOn()
		{
			this.SetEnabled(false);
			yield return new WaitForSeconds(this._delayBeforeLight);
			this.SetEnabled(true);
			yield break;
		}

		// Token: 0x0600579B RID: 22427
		private void GotBloody()
		{
			this.SetColor(this._torchBloodyColor);
		}

		// Token: 0x0600579C RID: 22428
		private void GotClean()
		{
			this.SetColor(this._torchBaseColor);
		}

		// Token: 0x0600579D RID: 22429
		private void manageShadows()
		{
			if (CoopPeerStarter.DedicatedHost || LocalPlayer.Transform == null || Scene.SceneTracker == null)
			{
				return;
			}
			if ((Scene.SceneTracker.activePlayerLights.Count < 3 || Scene.SceneTracker.activePlayerLights.Contains(base.gameObject)) && this._vis.localplayerDist < 60f)
			{
				if (!Scene.SceneTracker.activePlayerLights.Contains(base.gameObject))
				{
					Scene.SceneTracker.activePlayerLights.Add(base.gameObject);
				}
				TheForestQualitySettings.UserSettings.ApplyQualitySetting(this._mainLight, LightShadows.Hard);
				return;
			}
			if (Scene.SceneTracker.activePlayerLights.Contains(base.gameObject))
			{
				Scene.SceneTracker.activePlayerLights.Remove(base.gameObject);
			}
			TheForestQualitySettings.UserSettings.ApplyQualitySetting(this._mainLight, LightShadows.None);
		}

		// Token: 0x0600579E RID: 22430
		private void TorchLowerLight()
		{
			this.SetIntensity(this._lowBatteryIntensity1);
		}

		// Token: 0x0600579F RID: 22431
		private void TorchLowerLightMore()
		{
			this.SetIntensity(this._lowBatteryIntensity2);
		}

		// Token: 0x060057A0 RID: 22432
		private void TorchLowerLightEvenMore()
		{
			this.SetIntensity(this._lowBatteryIntensity3);
		}

		// Token: 0x060057A1 RID: 22433
		public void SetEnabled(bool enabled)
		{
			this._mainLight.enabled = enabled;
			if (this._fillLight)
			{
				this._fillLight.enabled = enabled;
			}
		}

		// Token: 0x060057A2 RID: 22434
		public void SetIntensity(float intensity)
		{
			this._mainLight.intensity = intensity;
			float num = intensity / 2f;
			if (intensity < 0.3f)
			{
				num = intensity / 3f;
			}
			if (num > 0.5f)
			{
				num = 0.5f;
			}
			if (this._fillLight)
			{
				this._fillLight.intensity = num;
			}
		}

		// Token: 0x060057A3 RID: 22435
		public void SetColor(Color color)
		{
			this._mainLight.color = color;
		}

		// Token: 0x060057A4 RID: 22436
		private void resetBatteryBool()
		{
			LocalPlayer.Animator.SetBool("noBattery", false);
		}

		// Token: 0x060057A5 RID: 22437
		private IEnumerator stashNoBatteryRoutine()
		{
			float clamp = 1f;
			new RandomRangeF(0.3f, 0.5f);
			float t = 0f;
			while (t < 1f)
			{
				float result = new RandomRangeF(0.3f, 0.5f);
				result *= clamp;
				this.SetIntensity(result);
				clamp = Mathf.Lerp(1f, 0f, t);
				t += Time.deltaTime / 2f;
				yield return null;
			}
			LocalPlayer.Inventory.StashLeftHand();
			yield break;
		}

		// Token: 0x04005D18 RID: 23832
		public Light _mainLight;

		// Token: 0x04005D19 RID: 23833
		public Light _fillLight;

		// Token: 0x04005D1A RID: 23834
		public Color _torchBaseColor;

		// Token: 0x04005D1B RID: 23835
		public Color _torchBloodyColor;

		// Token: 0x04005D1C RID: 23836
		public float _batterieCostPerSecond = 0.01f;

		// Token: 0x04005D1D RID: 23837
		public float _delayBeforeLight = 0.5f;

		// Token: 0x04005D1E RID: 23838
		public float _highBatteryIntensity = 4f;

		// Token: 0x04005D1F RID: 23839
		public RandomRangeF _lowBatteryIntensity1 = new RandomRangeF(4f, 3.5f);

		// Token: 0x04005D20 RID: 23840
		public RandomRangeF _lowBatteryIntensity2 = new RandomRangeF(3.5f, 3f);

		// Token: 0x04005D21 RID: 23841
		public RandomRangeF _lowBatteryIntensity3 = new RandomRangeF(3f, 2.5f);

		// Token: 0x04005D22 RID: 23842
		public bool _skipNoBatteryRoutine;

		// Token: 0x04005D23 RID: 23843
		public bool _manageDynamicShadows;

		// Token: 0x04005D24 RID: 23844
		private netPlayerVis _vis;

		// Token: 0x04005D25 RID: 23845
		private float _animCoolDown;

		// Token: 0x04005D26 RID: 23846
		private float _boolResetTimer;

		// Token: 0x04005D27 RID: 23847
		private bool _doingStash;
	}
}
