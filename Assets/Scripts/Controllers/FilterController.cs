using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FilterController : MonoBehaviour
{
    [SerializeField] private ScreenEditDeck deck;

    [SerializeField] private GameObject pepemonObject;
    [SerializeField] private GameObject defenseObject;
    [SerializeField] private GameObject offenseObject;

    public static FilterController Instance;

    public int currentFilter { get; private set; } = 0;

    public enum FilterType
    {
        Pepemon,
        Defense,
        Offense
    }

    private Dictionary<FilterType, bool> filterStates = new Dictionary<FilterType, bool>
    {
        { FilterType.Pepemon, false },
        { FilterType.Defense, false },
        { FilterType.Offense, false }
    };

    private void Awake()
    {
        Instance = this;
    }

    public bool AreAllFilterOff()
    {
        return !IsFilterOn(FilterType.Pepemon) && !IsFilterOn(FilterType.Defense)  && !IsFilterOn(FilterType.Offense);
    }

    public bool IsFilterOn(FilterType filterType)
    {
        return filterStates[filterType];
    }

    public void TogglePepemonFilter()
    {
        ToggleFilter(FilterType.Pepemon);
    }

    public void ToggleDefenseFilter()
    {
        ToggleFilter(FilterType.Defense);
    }

    public void ToggleOffenseFilter()
    {
        ToggleFilter(FilterType.Offense);
    }

    private void ToggleFilter(FilterType filterType)
    {
        filterStates[filterType] = !filterStates[filterType];
        UpdateCurrentFilter();
    }

    private void UpdateCurrentFilter()
    {
        int filterValue = 0;

        if (filterStates[FilterType.Pepemon]) filterValue |= 1 << 0;  // Set bit 0
        if (filterStates[FilterType.Defense]) filterValue |= 1 << 1;  // Set bit 1
        if (filterStates[FilterType.Offense]) filterValue |= 1 << 2;  // Set bit 2

        currentFilter = filterValue;

        // Update the filtered deck and toggle visibility for each filter-related GameObject
        deck.FilterCards(currentFilter);
        ToggleFilteredObjects();
    }

    public void ResetFilters()
    {
        // Reset all filter states to off
        filterStates[FilterType.Pepemon] = true; //pepemon is on by default
        filterStates[FilterType.Defense] = false;
        filterStates[FilterType.Offense] = false;

        // Update the filter and UI
        UpdateCurrentFilter();
    }

    private void ToggleFilteredObjects()
    {
        Color orange = new Color(1f, 0.5f, 0f); //orange

        pepemonObject.GetComponent<Image>().color = filterStates[FilterType.Pepemon] ? orange : Color.gray;
        defenseObject.GetComponent<Image>().color = filterStates[FilterType.Defense] ? orange : Color.gray;
        offenseObject.GetComponent<Image>().color = filterStates[FilterType.Offense] ? orange : Color.gray;
    }
}
