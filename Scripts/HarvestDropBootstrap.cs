using System.Collections;
using UnityEngine;

public class HarvestDropBootstrap : MonoBehaviour
{
	public GameObject woodDropPrefab;

	public GameObject stoneDropPrefab;

	public float mineableStoneMaxDimension = 25f;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void EnsureBootstrapExists()
	{
		if (!(Object.FindObjectOfType<HarvestDropBootstrap>() != null))
		{
			new GameObject("HarvestDropBootstrap").AddComponent<HarvestDropBootstrap>();
		}
	}

	private IEnumerator Start()
	{
		yield return null;
		ConfigureExistingResources();
	}

	public void ConfigureExistingResources()
	{
		ConfigureHarvestableCandidates();
		HarvestableResource[] array = Object.FindObjectsOfType<HarvestableResource>();
		foreach (HarvestableResource harvestableResource in array)
		{
			if (harvestableResource.resourceType != HarvestResourceType.Tree)
			{
				if (harvestableResource.resourceType == HarvestResourceType.Rock)
				{
					if (string.IsNullOrEmpty(harvestableResource.dropItemName))
					{
						harvestableResource.dropItemName = "Stone";
					}
					if (harvestableResource.dropCount <= 0)
					{
						harvestableResource.dropCount = 5;
					}
					if (harvestableResource.dropPrefab == null)
					{
						harvestableResource.dropPrefab = stoneDropPrefab;
					}
					if (!IsStoneDrop(harvestableResource.dropItemName) && (harvestableResource.dropPrefab == null || harvestableResource.dropPrefab == stoneDropPrefab))
					{
						harvestableResource.dropPrefab = harvestableResource.gameObject;
					}
					harvestableResource.requiredToolTier = Mathf.Max(harvestableResource.requiredToolTier, GetRequiredMiningTier(harvestableResource.dropItemName));
				}
			}
			else
			{
				if (string.IsNullOrEmpty(harvestableResource.dropItemName))
				{
					harvestableResource.dropItemName = "Wood";
				}
				if (harvestableResource.dropCount <= 0)
				{
					harvestableResource.dropCount = 5;
				}
				if (harvestableResource.dropPrefab == null)
				{
					harvestableResource.dropPrefab = woodDropPrefab;
				}
			}
		}
	}

	private void ConfigureHarvestableCandidates()
	{
		Transform[] array = Object.FindObjectsOfType<Transform>();
		foreach (Transform transform in array)
		{
			if (!(transform == null) && !HasActiveHarvestableParent(transform) && !(transform.GetComponentInParent<PlayerToolController>() != null) && !(transform.GetComponentInChildren<Renderer>(includeInactive: true) == null) && !(transform.GetComponentInChildren<Collider>(includeInactive: true) == null))
			{
				string text = transform.name.ToLowerInvariant();
				bool flag = text.Contains("tree") && !text.Contains("stump") && !text.Contains("trunk") && !text.Contains("log") && !text.Contains("terrain");
				string dropItemName;
				if (flag)
				{
					dropItemName = "Wood";
				}
				else if (!TryResolveMineableDrop(transform.gameObject, text, out dropItemName))
				{
					continue;
				}
				HarvestableResource harvestableResource = transform.gameObject.AddComponent<HarvestableResource>();
				harvestableResource.resourceType = ((!flag) ? HarvestResourceType.Rock : HarvestResourceType.Tree);
				harvestableResource.maxHealth = 5;
				harvestableResource.dropItemName = dropItemName;
				harvestableResource.dropCount = (flag ? 5 : ((dropItemName == "Stone") ? 4 : 3));
				harvestableResource.dropPrefab = (flag ? woodDropPrefab : ((dropItemName == "Stone") ? stoneDropPrefab : transform.gameObject));
				harvestableResource.requiredToolTier = ((!flag) ? GetRequiredMiningTier(dropItemName) : 0);
				harvestableResource.ResetHealth();
				harvestableResource.CaptureOriginalScale();
			}
		}
	}

	private static bool HasActiveHarvestableParent(Transform current)
	{
		if (current == null)
		{
			return false;
		}
		HarvestableResource[] componentsInParent = current.GetComponentsInParent<HarvestableResource>(includeInactive: true);
		foreach (HarvestableResource harvestableResource in componentsInParent)
		{
			if (harvestableResource != null && harvestableResource.enabled && harvestableResource.isActiveAndEnabled)
			{
				return true;
			}
		}
		return false;
	}

	private bool TryResolveMineableDrop(GameObject root, string lowerName, out string dropItemName)
	{
		dropItemName = "Stone";
		if (string.IsNullOrEmpty(lowerName) || root == null)
		{
			return false;
		}
		if (lowerName.Contains("pp_crystal"))
		{
			dropItemName = ResolveCrystalDropName(lowerName);
			return !string.IsNullOrEmpty(dropItemName);
		}
		if (lowerName.Contains("rockgrey1"))
		{
			dropItemName = "Stone";
			return true;
		}
		if (!LooksLikeLooseStone(lowerName) || LooksLikeNonMineableStone(lowerName))
		{
			return false;
		}
		if (TryGetRenderBounds(root, out var bounds) && bounds.size.x <= mineableStoneMaxDimension && bounds.size.y <= mineableStoneMaxDimension)
		{
			return bounds.size.z <= mineableStoneMaxDimension;
		}
		return false;
	}

	private static string ResolveCrystalDropName(string lowerName)
	{
		if (lowerName.Contains("green") || lowerName.Contains("emerald"))
		{
			return "Emerald Ore";
		}
		if (lowerName.Contains("blue") || lowerName.Contains("sapphire"))
		{
			return "Sapphire Ore";
		}
		if (lowerName.Contains("red") || lowerName.Contains("ruby"))
		{
			return "Ruby Ore";
		}
		if (lowerName.Contains("gold"))
		{
			return "Gold Ore";
		}
		if (lowerName.Contains("copper"))
		{
			return "Copper Ore";
		}
		if (lowerName.Contains("iron"))
		{
			return "Iron Ore";
		}
		if (lowerName.Contains("silver") || lowerName.Contains("coal"))
		{
			return "Coal Ore";
		}
		return null;
	}

	private static bool LooksLikeLooseStone(string lowerName)
	{
		if (!lowerName.Contains("rock"))
		{
			return lowerName.Contains("stone");
		}
		return true;
	}

	private static bool LooksLikeNonMineableStone(string lowerName)
	{
		if (!lowerName.Contains("stones&rocks") && !lowerName.Contains("crystals&ores&veins") && !lowerName.Contains("wall") && !lowerName.Contains("floor") && !lowerName.Contains("slab") && !lowerName.Contains("fragment") && !lowerName.Contains("path") && !lowerName.Contains("road") && !lowerName.Contains("bridge") && !lowerName.Contains("stair") && !lowerName.Contains("terrain"))
		{
			return lowerName.Contains("island");
		}
		return true;
	}

	private static int GetRequiredMiningTier(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return 1;
		}
		string text = itemName.ToLowerInvariant();
		if (text.Contains("emerald") || text.Contains("green"))
		{
			return 5;
		}
		if (text.Contains("sapphire") || text.Contains("blue"))
		{
			return 4;
		}
		if (text.Contains("ruby") || text.Contains("red") || text.Contains("gold") || text.Contains("copper"))
		{
			return 3;
		}
		if (text.Contains("iron") || text.Contains("coal") || text.Contains("silver"))
		{
			return 2;
		}
		return 1;
	}

	private static bool IsStoneDrop(string itemName)
	{
		string text = (itemName ?? string.Empty).ToLowerInvariant();
		if (!(text == "stone") && !text.Contains("stone"))
		{
			return text.Contains("rock");
		}
		return true;
	}

	private static bool TryGetRenderBounds(GameObject root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if (root == null)
		{
			return false;
		}
		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		bool flag = false;
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer == null) && renderer.enabled)
			{
				if (!flag)
				{
					bounds = renderer.bounds;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(renderer.bounds);
				}
			}
		}
		return flag;
	}
}
