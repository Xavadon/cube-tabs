using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Kit;
using _Project.Scripts.Gameplay.UI.Shop;
using UnityEngine;
using UnityEngine.UIElements;
using static _Project.Scripts.Architecture.Services.Localization.LocalizationKeys;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class EvolutionPanel
    {
        private const string OptionClass = "evo-option";
        private const string CardClass = "card";
        private const string PortraitClass = "portrait";
        private const string OptionPortraitClass = "evo-option__portrait";
        private const string InfoClass = "evo-option__info";
        private const string StatsClass = "evo-option__stats";
        private const string BuyClass = "evo-option__buy";
        private const string RowClass = "row";
        private const string StatClass = "stat";
        private const string StatIconClass = "stat__icon";
        private const string HpIconClass = "stat__icon--hp";
        private const string DamageIconClass = "stat__icon--damage";
        private const string SpeedIconClass = "stat__icon--speed";
        private const string ButtonClass = "btn";
        private const string GreenButtonClass = "btn--green";
        private const string CoinClass = "coin";
        private const string HealthFormat = "0";
        private const string DamageFormat = "0";
        private const string SpeedFormat = "0.#";

        private readonly VisualElement _root;
        private readonly VisualElement _options;
        private readonly IPlayerProgressService _progress;
        private readonly EvolutionCatalog _evolutionCatalog;
        private readonly IUnitPreviewService _previewService;
        private readonly IAudioService _audioService;
        private readonly ILocalizationService _localization;
        private readonly List<PreviewHandle> _shownPreviews = new();

        public EvolutionPanel(VisualElement root, VisualElement options, IPlayerProgressService progress,
            EvolutionCatalog evolutionCatalog, IUnitPreviewService previewService, IAudioService audioService,
            ILocalizationService localization)
        {
            _root = root;
            _options = options;
            _progress = progress;
            _evolutionCatalog = evolutionCatalog;
            _previewService = previewService;
            _audioService = audioService;
            _localization = localization;

            Hide();
        }

        public void Show(int instanceId, CharacterData currentData, int tierIndex)
        {
            ClearOptions();

            if (tierIndex < currentData.MaxTier)
            {
                ShowTierUpgrade(instanceId, currentData, tierIndex);
            }
            else
            {
                ShowEvolutions(instanceId, currentData);
            }
        }

        public void Hide()
        {
            _root.SetVisible(false);
            ClearOptions();
        }

        public void SetPreviewsActive(bool active)
        {
            foreach (PreviewHandle preview in _shownPreviews)
            {
                preview.SetActive(active);
            }
        }

        private void ClearOptions()
        {
            SetPreviewsActive(false);
            _shownPreviews.Clear();
            _options.Clear();
        }

        private void ShowTierUpgrade(int instanceId, CharacterData data, int tierIndex)
        {
            var nextTier = data.GetTier(tierIndex + 1);
            if (nextTier == null)
            {
                Hide();
                return;
            }

            var choice = new EvolutionChoice(instanceId, null, nextTier.EvolutionCost);
            AddOption(
                _localization.Get(Evolution.Tier, _localization.GetName(data), tierIndex + 2),
                _previewService.GetFullBody(data, tierIndex + 1),
                nextTier.Stats.Health,
                nextTier.Stats.Damage,
                nextTier.MoveSpeed,
                choice);

            _root.SetVisible(true);
        }

        private void ShowEvolutions(int instanceId, CharacterData currentData)
        {
            var evolutions = _evolutionCatalog.GetEvolutions(currentData.Id);

            if (evolutions.Length == 0)
            {
                Hide();
                return;
            }

            foreach (var evolution in evolutions)
            {
                var tier = evolution.Target.GetTier(0);
                var choice = new EvolutionChoice(instanceId, evolution.Target, evolution.Cost);

                AddOption(
                    _localization.GetName(evolution.Target),
                    _previewService.GetFullBody(evolution.Target, 0),
                    tier.Stats.Health,
                    tier.Stats.Damage,
                    tier.MoveSpeed,
                    choice);
            }

            _root.SetVisible(true);
        }

        private void AddOption(string unitName, PreviewHandle preview, float hp, float damage, float speed,
            EvolutionChoice choice)
        {
            var option = _options.AddChild(new VisualElement(), OptionClass);
            option.AddToClassList(CardClass);

            var portrait = option.AddChild(new VisualElement(), PortraitClass);
            portrait.AddToClassList(OptionPortraitClass);
            portrait.SetImage(preview.Texture);
            preview.SetActive(true);
            _shownPreviews.Add(preview);

            var info = option.AddChild(new VisualElement(), InfoClass);
            info.Add(new Label(unitName));

            var stats = info.AddChild(new VisualElement(), RowClass);
            stats.AddToClassList(StatsClass);
            AddStat(stats, HpIconClass, hp.ToString(HealthFormat));
            AddStat(stats, DamageIconClass, damage.ToString(DamageFormat));
            AddStat(stats, SpeedIconClass, speed.ToString(SpeedFormat));

            var buyButton = option.AddChild(new Button(), ButtonClass);
            buyButton.AddToClassList(GreenButtonClass);
            buyButton.AddToClassList(BuyClass);
            buyButton.Add(new Label(choice.Cost.ToString()));
            buyButton.AddChild(new VisualElement(), CoinClass);
            buyButton.userData = choice;
            buyButton.SetEnabled(_progress.CanAfford(choice.Cost));
            buyButton.RegisterCallback<ClickEvent>(OnOptionClicked);
        }

        private static void AddStat(VisualElement parent, string iconClass, string value)
        {
            var stat = parent.AddChild(new VisualElement(), StatClass);
            var icon = stat.AddChild(new VisualElement(), StatIconClass);
            icon.AddToClassList(iconClass);
            stat.Add(new Label(value));
        }

        private void OnOptionClicked(ClickEvent evt)
        {
            var choice = (EvolutionChoice)((VisualElement)evt.currentTarget).userData;
            _audioService.PlayUIClick();

            if (choice.Target == null)
            {
                _progress.UpgradeTier(choice.InstanceId);
            }
            else
            {
                _progress.EvolveUnit(choice.InstanceId, choice.Target, choice.Cost);
            }
        }

        private sealed class EvolutionChoice
        {
            public readonly int InstanceId;
            public readonly CharacterData Target;
            public readonly int Cost;

            public EvolutionChoice(int instanceId, CharacterData target, int cost)
            {
                InstanceId = instanceId;
                Target = target;
                Cost = cost;
            }
        }
    }
}
