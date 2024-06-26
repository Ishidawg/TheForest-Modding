using System;
using UnityEngine;

namespace Ceto
{
	// Token: 0x020001F4 RID: 500
	[AddComponentMenu("Ceto/Camera/UnderWaterPostEffect")]
	[RequireComponent(typeof(Camera))]
	public class UnderWaterPostEffect : MonoBehaviour
	{
		// Token: 0x06000EBB RID: 3771 RVA: 0x0000B474 File Offset: 0x00009674
		private void Start()
		{
			this.m_material = new Material(this.underWaterPostEffectSdr);
			this.m_query = new WaveQuery();
			// MOD - Remove m_blur object
		}

		// Token: 0x06000EBC RID: 3772 RVA: 0x000C61F8 File Offset: 0x000C43F8
		private void LateUpdate()
		{
			Camera component = base.GetComponent<Camera>();
			this.m_underWaterIsVisible = this.UnderWaterIsVisible(component);
			if (this.controlUnderwaterMode && Ocean.Instance != null && Ocean.Instance.UnderWater is UnderWater)
			{
				UnderWater underWater = Ocean.Instance.UnderWater as UnderWater;
				if (!this.m_underWaterIsVisible)
				{
					underWater.underwaterMode = UNDERWATER_MODE.ABOVE_ONLY;
					return;
				}
				underWater.underwaterMode = UNDERWATER_MODE.ABOVE_AND_BELOW;
			}
		}

		// Token: 0x06000EBD RID: 3773 RVA: 0x000C6268 File Offset: 0x000C4468
		// MOD - as it says, this is a function thats renders water
		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (this.underWaterPostEffectSdr == null || this.m_material == null || SystemInfo.graphicsShaderLevel < 30)
			{
				Graphics.Blit(source, destination);
				return;
			}
			if (Ocean.Instance == null || Ocean.Instance.UnderWater == null)
			{
				Graphics.Blit(source, destination);
				return;
			}
			if (!Ocean.Instance.gameObject.activeInHierarchy)
			{
				Graphics.Blit(source, destination);
				return;
			}
			if (Ocean.Instance.UnderWater.Mode != UNDERWATER_MODE.ABOVE_AND_BELOW)
			{
				Graphics.Blit(source, destination);
				return;
			}
			if (!this.m_underWaterIsVisible)
			{
				Graphics.Blit(source, destination);
				return;
			}
			Camera component = base.GetComponent<Camera>();
			float nearClipPlane = component.nearClipPlane;
			float farClipPlane = component.farClipPlane;
			float fieldOfView = component.fieldOfView;
			float aspect = component.aspect;
			Matrix4x4 identity = Matrix4x4.identity;
			float num = fieldOfView * 0.5f;
			Vector3 b = component.transform.right * nearClipPlane * Mathf.Tan(num * 0.017453292f) * aspect;
			Vector3 b2 = component.transform.up * nearClipPlane * Mathf.Tan(num * 0.017453292f);
			Vector3 vector = component.transform.forward * nearClipPlane - b + b2;
			float d = vector.magnitude * farClipPlane / nearClipPlane;
			vector.Normalize();
			vector *= d;
			Vector3 vector2 = component.transform.forward * nearClipPlane + b + b2;
			vector2.Normalize();
			vector2 *= d;
			Vector3 vector3 = component.transform.forward * nearClipPlane + b - b2;
			vector3.Normalize();
			vector3 *= d;
			Vector3 vector4 = component.transform.forward * nearClipPlane - b - b2;
			vector4.Normalize();
			vector4 *= d;
			identity.SetRow(0, vector);
			identity.SetRow(1, vector2);
			identity.SetRow(2, vector3);
			identity.SetRow(3, vector4);
			this.m_material.SetMatrix("_FrustumCorners", identity);
			Color value = Color.white;
			if (this.attenuateBySun)
			{
				value = Ocean.Instance.SunColor() * Mathf.Max(0f, Vector3.Dot(Vector3.up, Ocean.Instance.SunDir()));
			}
			this.m_material.SetColor("_MultiplyCol", value);
			RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default);
			this.CustomGraphicsBlit(source, temporary, this.m_material, 0);
			this.m_material.SetTexture("_BelowTex", temporary);
			// MOD - Remove all blut code from render function.
			Graphics.Blit(source, destination, this.m_material, 1);
			RenderTexture.ReleaseTemporary(temporary);
		}

		// Token: 0x06000EBE RID: 3774 RVA: 0x000C6550 File Offset: 0x000C4750
		private void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material mat, int pass)
		{
			RenderTexture.active = dest;
			mat.SetTexture("_MainTex", source);
			GL.PushMatrix();
			GL.LoadOrtho();
			mat.SetPass(pass);
			GL.Begin(7);
			GL.MultiTexCoord2(0, 0f, 0f);
			GL.Vertex3(0f, 0f, 3f);
			GL.MultiTexCoord2(0, 1f, 0f);
			GL.Vertex3(1f, 0f, 2f);
			GL.MultiTexCoord2(0, 1f, 1f);
			GL.Vertex3(1f, 1f, 1f);
			GL.MultiTexCoord2(0, 0f, 1f);
			GL.Vertex3(0f, 1f, 0f);
			GL.End();
			GL.PopMatrix();
		}

		// Token: 0x06000EBF RID: 3775 RVA: 0x000C6624 File Offset: 0x000C4824
		private bool UnderWaterIsVisible(Camera cam)
		{
			if (Ocean.Instance == null)
			{
				return false;
			}
			Vector3 position = cam.transform.position;
			if (this.disableOnClip)
			{
				this.m_query.posX = position.x;
				this.m_query.posZ = position.z;
				this.m_query.mode = QUERY_MODE.CLIP_TEST;
				Ocean.Instance.QueryWaves(this.m_query);
				if (this.m_query.result.isClipped)
				{
					return false;
				}
			}
			float num = Ocean.Instance.FindMaxDisplacement(true) + Ocean.Instance.level;
			if (position.y < num)
			{
				return true;
			}
			Matrix4x4 inverse = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
			for (int i = 0; i < 4; i++)
			{
				Vector4 vector = inverse * UnderWaterPostEffect.m_corners[i];
				vector.y /= vector.w;
				if (vector.y < num)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x04000E5B RID: 3675
		public bool disableOnClip = true;

		// Token: 0x04000E5C RID: 3676
		public bool controlUnderwaterMode;

		// Token: 0x04000E5D RID: 3677
		public bool attenuateBySun;

		// Token: 0x04000E5E RID: 3678
		public ImageBlur.BLUR_MODE blurMode;

		// Token: 0x04000E5F RID: 3679
		// MOD - This contructor has a DEFAULT value 3.
		[Range(0f, 4f)]
		public int blurIterations;

		// Token: 0x04000E60 RID: 3680
		// MOD - This contructor has a DEFAULT value 0.6f.
		[Range(0.5f, 1f)]
		private float blurSpread;

		// Token: 0x04000E61 RID: 3681
		public Shader underWaterPostEffectSdr;

		// Token: 0x04000E62 RID: 3682
		[HideInInspector]
		public Shader blurShader;

		// Token: 0x04000E63 RID: 3683
		private Material m_material;

		// Token: 0x04000E64 RID: 3684
		private ImageBlur m_imageBlur;

		// Token: 0x04000E65 RID: 3685
		private WaveQuery m_query;

		// Token: 0x04000E66 RID: 3686
		private bool m_underWaterIsVisible;

		// Token: 0x04000E67 RID: 3687
		private static readonly Vector4[] m_corners = new Vector4[]
		{
			new Vector4(-1f, -1f, -1f, 1f),
			new Vector4(1f, -1f, -1f, 1f),
			new Vector4(1f, 1f, -1f, 1f),
			new Vector4(-1f, 1f, -1f, 1f)
		};
	}
}
