using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Стратегия расчета святого урона (Faith).
    ///
    /// Особенности святого урона:
    /// - Минимальный порог урона - всегда наносит хотя бы 30% от базового урона
    /// - Эффективен против врагов с высоким сопротивлением
    /// - Тематически используется паладинами и клериками
    ///
    /// Формула: damage = max(baseDamage * 0.3, baseDamage * (1 - faithResist))
    ///
    /// Пример 1 (низкое сопротивление):
    /// - Базовый урон: 100
    /// - Faith сопротивление: 0.2 (20%)
    /// - Урон без порога: 100 * (1 - 0.2) = 80
    /// - Минимальный порог: 100 * 0.3 = 30
    /// - Итоговый урон: max(30, 80) = 80
    ///
    /// Пример 2 (высокое сопротивление):
    /// - Базовый урон: 100
    /// - Faith сопротивление: 0.9 (90%)
    /// - Урон без порога: 100 * (1 - 0.9) = 10
    /// - Минимальный порог: 100 * 0.3 = 30
    /// - Итоговый урон: max(30, 10) = 30  ← Гарантированный минимум!
    ///
    /// В будущем можно добавить:
    /// - Лечение союзников от части урона
    /// - Бонусный урон по нежити/демонам
    /// - Снятие дебаффов с цели
    /// </summary>
    public class FaithDamageStrategy : IDamageStrategy
    {
        // Faith урон всегда наносит минимум 30% от базового урона
        private const float MINIMUM_DAMAGE_PERCENT = 0.3f;

        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            // Обычный расчет урона с сопротивлением
            float normalDamage = baseDamage * (1f - resistances.FaithResist);

            // Минимальный гарантированный урон
            float minimumDamage = baseDamage * MINIMUM_DAMAGE_PERCENT;

            // Возвращаем максимум из двух значений
            float finalDamage = Mathf.Max(normalDamage, minimumDamage);

            // Логирование для отладки (можно убрать в продакшене)
            if (finalDamage == minimumDamage)
            {
                Debug.Log($"[Faith Damage] Сработал минимальный порог урона: {minimumDamage} " +
                         $"(вместо {normalDamage} из-за высокого сопротивления {resistances.FaithResist * 100}%)");
            }

            return finalDamage;
        }

        public string GetDamageTypeName() => "Faith";
    }
}
