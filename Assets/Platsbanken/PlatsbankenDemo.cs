using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlatsbankenDemo : MonoBehaviour {

    private Platsbanken platsbanken;

    [Header("Occupation Field")]
    [SerializeField] private ScrollRect occupationFieldScrollRect;
    [SerializeField] private Button occupationFieldButton;
    [SerializeField] private Text occupationFieldTitle;

    [Header("Occupation Group")]
    [SerializeField] private ScrollRect occupationGroupScrollRect;
    [SerializeField] private Button occupationGroupButton;
    [SerializeField] private Text occupationGroupTitle;

    [Header("Occupation")]
    [SerializeField] private ScrollRect occupationScrollRect;
    [SerializeField] private Button occupationButton;
    [SerializeField] private Text occupationTitle;

    [Header("Skills")]
    [SerializeField] private Text skillsText;

    [Header("Ads")]
    [SerializeField] private ScrollRect adScrollRect;
    [SerializeField] private Button adButton;
    [SerializeField] private Text adInfoText;
    [SerializeField] private Button searchButton;
    [SerializeField] private InputField searchText;

    [Header("Job")]
    [SerializeField] private Text jobDescriptionText;
    [SerializeField] private Button viewAdOnWebButton;

    private List<GameObject> occupationFieldButtons = new List<GameObject>();
    private List<GameObject> occupationGroupButtons = new List<GameObject>();
    private List<GameObject> occupationButtons = new List<GameObject>();
    private List<GameObject> adButtons = new List<GameObject>();

    private string currentOccupationField;
    private string currentOccupationGroup;

    void Start() {

        occupationGroupButton.gameObject.SetActive(false);
        occupationButton.gameObject.SetActive(false);
        adButton.gameObject.SetActive(false);

        searchButton.onClick.AddListener(() => {
            ResetAds();
            ResetOccupationGroup();
            ResetOccupation();
            ResetOccupationField();
            ResetSkills();
            var searchTermArray = searchText.text.Split(',');
            var list = new List<string>(searchTermArray);
            LoadAndShowAds(list);
        });

        skillsText.text = "";
        jobDescriptionText.text = "";
        adInfoText.text = "";

        platsbanken = GetComponent<Platsbanken>();
        if(platsbanken != null) {
            MainMode();
            //PrintRandomAd();
        } else {
            Debug.LogWarning("The Platsbanken monobehaviour needs to be attached to this gameobject!");
        }

    }

    private void MainMode() {
        PopulateOccupationFields();
    }

    private void TestAdSearch(List<string> searchTerms) {
        LoadAndShowAds(searchTerms);
    }

    private async void PrintRandomAd() {

        var occupationFields = platsbanken.GetOccupationFields();
        var occupationField = RandomFromList(occupationFields);
        var occupationGroups = platsbanken.GetOccupationGroups(occupationField);
        var occupationGroup = RandomFromList(occupationGroups);
        var occupations = platsbanken.GetOccupations(occupationField, occupationGroup);
        var occupation = RandomFromList(occupations);
        var skills = await platsbanken.GetSkillsAsync(occupationField, occupationGroup);

        print(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
        print("--------------");
        print("FIELD = " + occupationField);
        print("--------------");
        print("GROUP = " + occupationGroup);
        print("--------------");
        print("OCCUPATION = " + occupation);
        print("--------------");
        var index = 1;
        foreach (var skill in skills) {
            print("SKILL " + index++ + "/" + skills.Count + " = " + skill);
        }

        var num = await platsbanken.GetNumberOfAds(occupationField, occupationGroup);
        print("--------------");
        print(num + " ads posted for '" + occupationGroup + "' in the last " + platsbanken.SearchSpanInDays + " days");

        var ads = await platsbanken.GetAds(occupationField, occupationGroup, 1);
        if (ads.Count > 0) {
            var ad = ads[0];
            print("--------------");
            print("AD OF THE DAY: " + ad.headline);
        }
        print(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
    }

    private void PopulateOccupationFields() {
        occupationFieldButton.gameObject.SetActive(true);
        var occupationFields = platsbanken.GetOccupationFields();
        var currentPos = occupationFieldButton.transform.localPosition;
        foreach (var field in occupationFields) {
            var button = Instantiate(occupationFieldButton, occupationFieldButton.transform.parent);
            button.gameObject.transform.localPosition = currentPos;
            button.GetComponentInChildren<Text>().text = field;
            button.onClick.AddListener(() => {
                ResetOccupationGroup();
                ResetOccupation();
                currentOccupationField = field;
                occupationFieldTitle.text = field;
                PopulateOccupationGroups(currentOccupationField);
            });
            currentPos.y -= 30;
        }
        occupationFieldButton.gameObject.SetActive(false);
    }

    private void PopulateOccupationGroups(string occupationField) {
        ResetOccupationGroup();
        occupationGroupButton.gameObject.SetActive(true);
        var occupationGroups = platsbanken.GetOccupationGroups(occupationField);
        var currentPos = occupationGroupButton.transform.localPosition;
        foreach (var group in occupationGroups) {
            var button = Instantiate(occupationGroupButton, occupationGroupButton.transform.parent);
            button.gameObject.transform.localPosition = currentPos;
            button.GetComponentInChildren<Text>().text = group;
            button.onClick.AddListener(() => {
                ResetOccupation();
                currentOccupationGroup = group;
                occupationGroupTitle.text = group;
                PopulateOccupations(currentOccupationField, currentOccupationGroup);
                PopulateSkillsTextAsync(currentOccupationField, currentOccupationGroup);
                LoadAndShowAds(currentOccupationField, currentOccupationGroup);
            });
            currentPos.y -= 30;
            occupationGroupButtons.Add(button.gameObject);
        }
        occupationGroupButton.gameObject.SetActive(false);
    }

    private void PopulateOccupations(string occupationField, string occupationGroup) {
        ResetOccupation();
        occupationButton.gameObject.SetActive(true);
        var occupations = platsbanken.GetOccupations(occupationField, occupationGroup);
        var currentPos = occupationButton.transform.localPosition;
        foreach (var occ in occupations) {
            var button = Instantiate(occupationButton, occupationButton.transform.parent);
            button.gameObject.transform.localPosition = currentPos;
            button.GetComponentInChildren<Text>().text = occ;
            button.onClick.AddListener(() => {
                occupationTitle.text = occ;
            });
            currentPos.y -= 30;
            occupationButtons.Add(button.gameObject);
        }
        occupationButton.gameObject.SetActive(false);
    }

    private async void LoadAndShowAds(string occupationField, string occupationGroup) {
        var ads = await platsbanken.GetAds(occupationField, occupationGroup);
        PopulateAds(ads);
    }

    private async void LoadAndShowAds(List<String> searchTerms) {
        var ads = await platsbanken.GetAds(searchTerms);
        PopulateAds(ads);
    }

    private void PopulateAds(List<JobAdClasses.Hit> ads) {
        ResetAds();
        if(ads.Count == 0) {
            adInfoText.text = "No ads for this selection!";
            return;
        }
        adInfoText.text = "";
        adButton.gameObject.SetActive(true);
        var currentPos = adButton.transform.localPosition;
        var cnt = 1;
        foreach (var ad in ads) {
            var button = Instantiate(adButton, adButton.transform.parent);
            button.gameObject.transform.localPosition = currentPos;
            button.GetComponentInChildren<Text>().text = cnt++ + "-" + ad.headline;
            button.onClick.AddListener(() => {
                ShowAd(ad);
            });
            currentPos.y -= 30;
            adButtons.Add(button.gameObject);
        }
        adButton.gameObject.SetActive(false);
    }

    private async void PopulateSkillsTextAsync(string currentOccupationField, string currentOccupationGroup) {
        skillsText.text = "Loading...";
        var skills = await platsbanken.GetSkillsAsync(currentOccupationField, currentOccupationGroup);
        skillsText.text = "";
        Canvas.ForceUpdateCanvases();
        foreach (var skill in skills) {
            skillsText.text += (skill + Environment.NewLine);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void ShowAd(JobAdClasses.Hit ad) {
        jobDescriptionText.text = "";
        Canvas.ForceUpdateCanvases();
        jobDescriptionText.text = ad.headline + Environment.NewLine + Environment.NewLine + ad.description.text;
        viewAdOnWebButton.onClick.RemoveAllListeners();
        viewAdOnWebButton.onClick.AddListener(() => {
            Application.OpenURL(ad.webpage_url);
        });
        Canvas.ForceUpdateCanvases();
    }

    private void ResetOccupationField() {
        currentOccupationField = null;
        occupationFieldTitle.text = "Occupation Field...";
    }

    private void ResetOccupationGroup() {
        currentOccupationGroup = null;
        foreach (var go in occupationGroupButtons) {
            Destroy(go);
        }
        occupationGroupButtons.Clear();
        occupationGroupTitle.text = "Occupation Group...";
    }

    private void ResetOccupation() {
        foreach (var go in occupationButtons) {
            Destroy(go);
        }
        occupationButtons.Clear();
        occupationTitle.text = "Occupation";
    }

    private void ResetAds() {
        foreach (var go in adButtons) {
            Destroy(go);
        }
        adButtons.Clear();
        jobDescriptionText.text = "";
    }

    private void ResetSkills() {
        skillsText.text = "";
    }

    private string RandomFromList(List<String> list) {
        if (list == null || list.Count == 0) {
            return "NULL";
        }
        return list[UnityEngine.Random.Range(0, list.Count - 1)];
    }

}
