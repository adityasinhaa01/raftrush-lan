using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Cosmetics
{
	public static class SkinnedGarmentBinder
	{
		public static Dictionary<string, Transform> BuildBoneMap(Transform skeletonRoot)
		{
			Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>();
			if (skeletonRoot == null)
			{
				return dictionary;
			}
			Transform[] componentsInChildren = skeletonRoot.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				dictionary[transform.name] = transform;
			}
			return dictionary;
		}

		public static SkinnedMeshRenderer Rebind(SkinnedMeshRenderer smr, Dictionary<string, Transform> boneMap, Transform fallbackRoot)
		{
			if (smr == null || boneMap == null)
			{
				return smr;
			}
			Transform[] bones = smr.bones;
			Transform[] array = new Transform[bones.Length];
			for (int i = 0; i < bones.Length; i++)
			{
				if (bones[i] != null && boneMap.TryGetValue(bones[i].name, out var value))
				{
					array[i] = value;
				}
				else
				{
					array[i] = ((bones[i] != null) ? bones[i] : fallbackRoot);
				}
			}
			smr.bones = array;
			if (smr.rootBone != null && boneMap.TryGetValue(smr.rootBone.name, out var value2))
			{
				smr.rootBone = value2;
			}
			else if (fallbackRoot != null)
			{
				smr.rootBone = fallbackRoot;
			}
			return smr;
		}
	}
}
