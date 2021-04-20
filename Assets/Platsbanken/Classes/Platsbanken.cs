using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public class Platsbanken : MonoBehaviour {

    // You must have a valid JobTech API key to proceed
    // Get your own API key at https://apirequest.jobtechdev.se/

    [Header("Local JSONS files:")]
    [SerializeField] private TextAsset ssykTaxonomyJson;
    [SerializeField] private TextAsset occupationsJson;
    [SerializeField] private TextAsset skillsToOccupationGroupsJson;

    [Header("Your personal JobTech API key:")]
    [SerializeField] private string jobTechAPIKey;

    [Header("How many days to search back in time:")]
    [SerializeField] private int searchSpanInDays = 14;
    public int SearchSpanInDays => searchSpanInDays;

    private readonly string jobTechAPIUrl = "https://jobsearch.api.jobtechdev.se/search";

    private SkillsAndOccupations skillsToAds;
    private Taxonomy taxonomy;

    void Awake() {
        taxonomy = new Taxonomy(ssykTaxonomyJson, occupationsJson);
        skillsToAds = new SkillsAndOccupations(skillsToOccupationGroupsJson);
    }

    public List<String> GetOccupationFields(){
        return LoadSSYKLevel1();
    }

    public List<String> GetOccupationGroups(string occupationFieldName) {
        return LoadSSYKLevel2(occupationFieldName);
    }

    public List<String> GetOccupations(string occupationFieldName, string occupationGroupName) {
        return LoadSSYKLevel3(occupationFieldName, occupationGroupName);
    }
    public async Task<List<string>> GetSkillsAsync(string occupationFieldName, string occupationGroupName) {
        return await LoadSSYKLevel4(occupationFieldName, occupationGroupName);
    }

    private List<String> LoadSSYKLevel1() {
        // Occupation Fields
        var list = new List<String>();
        foreach (var field in taxonomy.OccupationFields) {
            list.Add(field.Key);
        }
        return list;
    }

    private List<String> LoadSSYKLevel2(string occupationField) {
        // Occupation Groups
        var list = new List<String>();
        foreach (var field in taxonomy.OccupationFields) {
            if(field.Key.Equals(occupationField)){
                foreach (var group in field.Value.occupationGroups) {
                    list.Add(group.Key);
                }
            }
        }
        return list;
    }

    private List<String> LoadSSYKLevel3(string occupationField, string occupationGroup) {
        // Occupations
        var list = new List<String>();
        foreach (var field in taxonomy.OccupationFields) {
            if (field.Key.Equals(occupationField)) {
                foreach (var group in field.Value.occupationGroups) {
                    if (group.Key.Equals(occupationGroup)) {
                        foreach (var occupation in group.Value.occupations) {
                            list.Add(occupation.Key);
                        }
                    }
                }
            }
        }
        return list;
    }

    private async Task<List<String>> LoadSSYKLevel4(string occupationField, string occupationGroup) {
        // Skills - loaded dynamically through the JobTech API
        var list = new List<String>();
        foreach (var field in taxonomy.OccupationFields) {
            if (field.Key.Equals(occupationField)) {
                foreach (var group in field.Value.occupationGroups) {
                    if (group.Key.Equals(occupationGroup)) {
                        var skillConcepts = await taxonomy.GetSkillConcepts(group.Value.conceptID, jobTechAPIKey);
                        foreach (var concept in skillConcepts.data.concepts?[0].skills.OrderBy(d => d.preferred_label)) {
                            list.Add(concept.preferred_label);
                        }
                    }
                }
            }
        }
        return list;
    }

    public String GetOccupationGroupID(string occupationField, string occupationGroup) {
        foreach (var field in taxonomy.OccupationFields) {
            if (field.Key.Equals(occupationField)) {
                foreach (var group in field.Value.occupationGroups) {
                    if (group.Key.Equals(occupationGroup)) {
                        return group.Value.conceptID;
                    }
                }
            }
        }
        return "";
    }

    public async Task<List<JobAdClasses.Hit>> GetAds(List<String> searchTerms, int numberOfAdsToLoad = 99) {
        var minutesToSearch = 60 * 24 * searchSpanInDays;
        var urlRequest = jobTechAPIUrl
            + "?published-after=" + minutesToSearch
            + "&q=" + String.Join(" ", searchTerms)
            + "&resdet=" + "full" // use "brief" here to get the ads without body text
            + "&offset=0"
            + "&limit=" + numberOfAdsToLoad;

        return await GetAdsRequest(urlRequest);
    }

    public async Task<List<JobAdClasses.Hit>> GetAds(string occupationField, string occupationGroup, int numberOfAdsToLoad = 99) {
        var occupationGroupID = GetOccupationGroupID(occupationField, occupationGroup);
        var minutesToSearch = 60 * 24 * searchSpanInDays;
        var urlRequest = jobTechAPIUrl
            + "?published-after=" + minutesToSearch
            + "&occupation-group=" + occupationGroupID
            + "&resdet=" + "full" // use "brief" here to get the ads without body text
            + "&offset=0"
            + "&limit=" + numberOfAdsToLoad;

        return await GetAdsRequest(urlRequest);
    }

    private async Task<List<JobAdClasses.Hit>> GetAdsRequest(string urlRequest) {
        HttpWebRequest request = HttpWebRequest.CreateHttp(urlRequest);
        request.Headers.Add("api-key:" + jobTechAPIKey);
        try {
            WebResponse response = await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            string json = new StreamReader(responseStream).ReadToEnd();
            JobAdClasses.Ads data = JsonUtility.FromJson<JobAdClasses.Ads>(json);

            return data.hits;
        } catch (WebException e) {
            Debug.Log("Error when trying to load ads: " + e.Message);
        }
        return new List<JobAdClasses.Hit>();
    }

    public async Task<int> GetNumberOfAds(string occupationField, string occupationGroup) {
        // Return ONLY the total number of ads for this timespan
        var occupationGroupID = GetOccupationGroupID(occupationField, occupationGroup);
        var minutesToSearch = 60 * 24 * searchSpanInDays;
        var urlRequest = jobTechAPIUrl
            + "?published-after=" + minutesToSearch
            + "&occupation-group=" + occupationGroupID
            + "&resdet=" + "brief" // use "full" here to get the complete ads
            + "&offset=0"
            + "&limit=0";

        HttpWebRequest request = HttpWebRequest.CreateHttp(urlRequest);
        request.Headers.Add("api-key:" + jobTechAPIKey);
        try {
            WebResponse response = await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            string json = new StreamReader(responseStream).ReadToEnd();
            JobAdClasses.Ads data = JsonUtility.FromJson<JobAdClasses.Ads>(json);
            return data.total.value;
        } catch (WebException e) {
            Debug.Log("Error when trying to count ads: " + e.Message);
        }
        return 0;
    }

}
