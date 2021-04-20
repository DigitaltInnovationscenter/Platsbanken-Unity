using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This uses a local JSON to connect skills back to occupation groups
// Pass a List<String> of skill ids to GetOccupationGroups() to get suggested occupation groups (sorted by frequency)
// Send each of those occupation-group IDs into GetAdsForOccupationGroup() to fetch relevant ads

public class SkillsAndOccupations {

    private SkillsData skillsData;

    public SkillsAndOccupations(TextAsset skillsToOccupationGroupsJson) {
        ImportSkillsData(skillsToOccupationGroupsJson);
    }

    private void ImportSkillsData(TextAsset skillsToOccupationGroupsJson) {
        var json = skillsToOccupationGroupsJson.ToString();
        skillsData = JsonUtility.FromJson<SkillsData>(json);
    }

    public List<String> GetSkillIDs(string occupationGroupID) {
        var result = new List<string>();
        foreach (var skill in skillsData.data.concepts) {
            foreach (var occGroup in skill.occupation_groups) {
                if (occGroup.id.Equals(occupationGroupID)) {
                    result.Add(skill.id);
                }
            }
        }
        return result;
    }

    private List<String> GetRandomSkillIDs(int number) {
        var list = new List<string>();
        var num = skillsData.data.concepts.Count;
        for (int i = 0; i < number; i++) {
            var randomSkill = skillsData.data.concepts[UnityEngine.Random.Range(0, num)];
            list.Add(randomSkill.id);
        }
        return list;
    }

    private Concept GetSkill(string id) {
        foreach (var concept in skillsData.data.concepts) {
            if(concept.id.Equals(id)) {
                return concept;
            }
        }
        return null;
    }

    public string GetOccupationTitle(string id) {
        foreach (var skill in skillsData.data.concepts) {
            foreach (var occupation in skill.occupation_groups) {
                if(occupation.id.Equals(id)) {
                    return occupation.preferred_label;
                }
            }
        }
        return "";
    }

    public List<OccupationGroupResult> GetOccupationGroups(List<String> skillIDs, int numberOfResults = 9999) {
        var results = new List<OccupationGroupResult>();
        foreach (var skillID in skillIDs) {
            var skillObj = GetSkill(skillID);
            if(skillObj != null) {
                foreach (var group in skillObj.occupation_groups) {
                    var index = results.FindIndex(r => r.id == group.id);
                    if (index >= 0) {
                        results[index].count++;
                    } else {
                        var newResult = new OccupationGroupResult() {
                            preferred_label = group.preferred_label,
                            id = group.id,
                            count = 1
                        };
                        results.Add(newResult);
                    }
                }
            }
        }
        var sortedList = results.OrderByDescending(r => r.count).ToList();
        while(sortedList.Count > numberOfResults) {
            sortedList.RemoveAt(sortedList.Count - 1);
        }
        return sortedList;
    }

}

public class OccupationGroupResult
{
    public string preferred_label;
    public string id;
    public int count;
}

[Serializable]
public class OccupationGroups
{
    public string id;
    public string preferred_label;
    public string type;
}

[Serializable]
public class Concept
{
    public string preferred_label;
    public string id;
    public List<OccupationGroups> occupation_groups;
}

[Serializable]
public class Data
{
    public List<Concept> concepts;
}

[Serializable]
public class SkillsData
{
    public Data data;
}

