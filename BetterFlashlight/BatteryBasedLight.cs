using System;
using System.Collections;
using Bolt;
using TheForest.Utils;
using UnityEngine;

namespace TheForest.Items.World
{
	// Token: 0x02000FCC RID: 4044
	[AddComponentMenu("Items/World/Battery Based Light")]
	public class BatteryBasedLight : EntityBehaviour<IPlayerState>
	{
		// Token: 0x0600680F RID: 26639
		private void Awake()
		{
			this._vis = base.transform.root.GetComponent<netPlayerVis>();
			this.SetColor(this._torchBaseColor);
            // MOD - Custom values
			if (this._mainLight != null)
			{
				this._mainLight.range = 260f;
				this._mainLight.spotAngle = 135f;
				this._mainLight.intensity = 22f;
			}
		}

		// Token: 0x06006810 RID: 26640 RVA: 0x002B4D34 File Offset: 0x002B2F34
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

		// Token: 0x06006811 RID: 26641 RVA: 0x002B4DA4 File Offset: 0x002B2FA4
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

		// Token: 0x06006812 RID: 26642 RVA: 0x002B4E50 File Offset: 0x002B3050
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
								this._animCoolDown = Time.time + (float)UnityEngine.Random.Range(30, 60);
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

		// Token: 0x06006813 RID: 26643 RVA: 0x00042350 File Offset: 0x00040550
		private IEnumerator DelayedLightOn()
		{
			this.SetEnabled(false);
			yield return new WaitForSeconds(this._delayBeforeLight);
			this.SetEnabled(true);
			yield break;
		}

		// Token: 0x06006814 RID: 26644 RVA: 0x0004235F File Offset: 0x0004055F
		private void GotBloody()
		{
			this.SetColor(this._torchBloodyColor);
		}

		// Token: 0x06006815 RID: 26645 RVA: 0x0004236D File Offset: 0x0004056D
		private void GotClean()
		{
			this.SetColor(this._torchBaseColor);
		}

		// Token: 0x06006816 RID: 26646 RVA: 0x002B50B0 File Offset: 0x002B32B0
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

		// Token: 0x06006817 RID: 26647 RVA: 0x0004237B File Offset: 0x0004057B
		private void TorchLowerLight()
		{
			this.SetIntensity(this._lowBatteryIntensity1);
		}

		// Token: 0x06006818 RID: 26648 RVA: 0x0004238E File Offset: 0x0004058E
		private void TorchLowerLightMore()
		{
			this.SetIntensity(this._lowBatteryIntensity2);
		}

		// Token: 0x06006819 RID: 26649 RVA: 0x000423A1 File Offset: 0x000405A1
		private void TorchLowerLightEvenMore()
		{
			this.SetIntensity(this._lowBatteryIntensity3);
		}

		// Token: 0x0600681A RID: 26650 RVA: 0x000423B4 File Offset: 0x000405B4
		public void SetEnabled(bool enabled)
		{
			this._mainLight.enabled = enabled;
			if (this._fillLight)
			{
				this._fillLight.enabled = enabled;
			}
		}

		// Token: 0x0600681B RID: 26651 RVA: 0x002B5198 File Offset: 0x002B3398
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

		// Token: 0x0600681C RID: 26652 RVA: 0x000423DB File Offset: 0x000405DB
		public void SetColor(Color color)
		{
			this._mainLight.color = color;
		}

		// Token: 0x0600681D RID: 26653 RVA: 0x000423E9 File Offset: 0x000405E9
		private void resetBatteryBool()
		{
			LocalPlayer.Animator.SetBool("noBattery", false);
		}

		// Token: 0x0600681E RID: 26654 RVA: 0x000423FB File Offset: 0x000405FB
		private IEnumerator stashNoBatteryRoutine()
		{
			float clamp = 1f;
			new RandomRangeF(0.3f, 0.5f);
			float t = 0f;
			while (t < 1f)
			{
				float num = new RandomRangeF(0.3f, 0.5f);
				num *= clamp;
				this.SetIntensity(num);
				clamp = Mathf.Lerp(1f, 0f, t);
				t += Time.deltaTime / 2f;
				yield return null;
			}
			LocalPlayer.Inventory.StashLeftHand();
			yield break;
		}

		// Token: 0x04006D78 RID: 28024
		public Light _mainLight;

		// Token: 0x04006D79 RID: 28025
		public Light _fillLight;

		// Token: 0x04006D7A RID: 28026
		public Color _torchBaseColor;

		// Token: 0x04006D7B RID: 28027
		public Color _torchBloodyColor;

		// Token: 0x04006D7C RID: 28028
        // MOD - Reduce battery cost
		public float _batterieCostPerSecond = 0.01f;

		// Token: 0x04006D7D RID: 28029
		public float _delayBeforeLight = 0.5f;

		// Token: 0x04006D7E RID: 28030
        // MOD - Remove the values
		public float _highBatteryIntensity;

		// Token: 0x04006D7F RID: 28031
        // MOD - Remove the values
		public RandomRangeF _lowBatteryIntensity1;

		// Token: 0x04006D80 RID: 28032
        // MOD - Remove the values
		public RandomRangeF _lowBatteryIntensity2;

		// Token: 0x04006D81 RID: 28033
        // MOD - Remove the values
		public RandomRangeF _lowBatteryIntensity3;

		// Token: 0x04006D82 RID: 28034
		public bool _skipNoBatteryRoutine;

		// Token: 0x04006D83 RID: 28035
		public bool _manageDynamicShadows;

		// Token: 0x04006D84 RID: 28036
		private netPlayerVis _vis;

		// Token: 0x04006D85 RID: 28037
		private float _animCoolDown;

		// Token: 0x04006D86 RID: 28038
		private float _boolResetTimer;

		// Token: 0x04006D87 RID: 28039
		private bool _doingStash;
	}
}
