# Game Balance Design Prompt

Ты — геймдизайнер, специализирующийся на балансе auto-battler / idle-army игр. Тебе нужно спроектировать полный баланс для мобильной игры на Yandex Games.

---

## Описание игры

**Жанр:** Auto-battler с армией юнитов. Игрок собирает армию, покупает и эволюционирует юнитов, проходит уровни.

**Геймплей:**
- Игрок формирует армию из юнитов разных классов
- Бой автоматический — юниты действуют по AI (Behavior Tree)
- Между боями: покупка юнитов, эволюция, улучшение армии
- Враги спавнятся волнами, нужно набрать определённое число убийств для прохождения уровня
- Уровни переигрываются для фарма золота

**Целевая аудитория:** Casual mobile (Yandex Games), сессии 5-15 минут.

---

## Структура данных (точная, из Unity SO)

### CharacterData (юнит)

```
CharacterData:
  Id: int                  — уникальный идентификатор
  Name: string             — название юнита
  Price: int               — стоимость покупки в магазине (золото)
  Tiers: TierData[]        — массив тиров (обычно 3: tier 0, 1, 2)
```

### TierData (тир юнита)

Каждый тир — полный набор характеристик юнита на данном уровне развития:

```
TierData:
  Stats:
    Health: float          — очки здоровья
    Mana: float            — мана (не используется в бою, зарезервировано)
    Stamina: float         — стамина (не используется в бою, зарезервировано)
  Resistances:
    PhysicalResist: float  — сопротивление физическому урону (0.0–1.0, где 0.1 = 10%)
    MagicResist: float     — сопротивление магическому урону
    FireResist: float      — сопротивление огненному урону
    FaithResist: float     — сопротивление святому урону
  WeaponData[]:            — оружие (может быть null для магов)
    Damage: float          — урон оружия за удар
    DamageType: enum       — Physical | Magic | Fire | Faith
  BrainData: enum          — тип AI: Melee | Range | Ability | Player
  Ability: AbilityData     — способность (null для чистых мили)
  AnimationType: enum      — None | RangeAttack | AbilityAttack
  MoveSpeed: float         — скорость передвижения
  KillReward: int          — золото за убийство этого юнита (для врагов)
  EvolutionCost: int       — стоимость улучшения тира (0 если нельзя улучшить)
```

### Типы способностей

```
ProjectileAbility:         — одиночный снаряд (стрелы, маг.снаряды)
  Damage: float
  DamageType: enum
  Speed: float             — скорость полёта

AreaAbility:               — AoE урон вокруг кастера
  Damage: float
  DamageType: enum
  Radius: float            — радиус поражения
  Duration: float          — длительность зоны
```

### AI поведение (Brain)

```
MeleeBrain:                — ближний бой
  DetectionRadius: 10      — радиус обнаружения врага
  AttackRange: 1.75        — дальность атаки
  AttackCooldown: 0.5s     — кулдаун между ударами
  WindUp: 0.3s             — замах
  AttackDuration: 1.0s     — длительность атаки

RangeBrain:                — дальний бой (лучники, маги)
  DetectionRadius: 15
  FleeRange: 5             — дистанция бегства (убегает если враг ближе)
  AttackRange: 10
  AttackCooldown: 2.0s
  WindUp: 0.3s
  AttackDuration: 2.0s

AbilityBrain(extends Range):— маги с AoE
  Те же параметры, но использует Ability вместо RangedAttack
```

### Эволюция

```
EvolutionCatalog:
  Entries[]:
    Source: CharacterData        — исходный юнит
    Options[]:
      Target: CharacterData      — юнит, в которого эволюционирует
      Cost: int                  — стоимость эволюции (золото)
```

Дерево эволюции ветвистое: один юнит может эволюционировать в 2-3 варианта.

### Уровни

```
LevelConfig:
  LevelName: string
  LevelIndex: int
  KillsToComplete: int          — сколько убийств нужно для прохождения
  Enemies: SpawnEntry[]
    CharacterData                — тип врага
    TierIndex: int               — тир врага
    Count: int                   — количество одновременно на арене
```

Враги респавнятся — Count это не общее число, а сколько одновременно на поле.

### Экономика магазина

```
ShopCatalog:
  StartingGold: int              — золото в начале игры
  BaseArmySlots: int             — стартовые слоты армии
  MaxArmySlots: int              — максимум слотов
  SlotUpgradeCost: int           — цена за дополнительный слот
  AvailableUnits: CharacterData[]— юниты доступные для покупки
  UniqueHeroes: CharacterData[]  — уникальные герои (не эволюционируют)

ShopItem (реальные покупки):
  RewardType: Gold | ArmySlot
  RewardAmount: int
```

---

## Классы юнитов и роли

Используй эту классификацию:

| Роль | Brain | Оружие | Ability | Характеристика |
|------|-------|--------|---------|----------------|
| Tank (Warrior, Knight, Paladin) | Melee | Sword/Mace | null | Высокий HP, средний урон, низкая скорость |
| DPS Melee (Berserk, Rogue) | Melee | Dual/Dagger | null | Средний HP, высокий урон, высокая скорость |
| Archer | Range | null | Projectile (Physical) | Низкий HP, средний урон, высокая скорость, быстрый кулдаун |
| Mage (Fire, Dark, Apprentice) | Ability | null | Projectile/Area (Magic/Fire) | Очень низкий HP, высокий урон, низкая скорость |
| Support (Heal Mage) | Ability | null | Area (Faith) | Низкий HP, лечение вместо урона, средняя скорость |

---

## Референсные числа (текущие в проекте)

### Player юниты

**Warrior (melee tank, Id: 8):**
- Tier 0: HP 200, MoveSpeed 5.0, Weapon: Sword 50 Physical
- Tier 1: HP 250, MoveSpeed 5.2
- Tier 2: HP 300, MoveSpeed 5.4
- Price: 100

**Archer (ranged, Id: 9):**
- Tier 0: HP 150, MoveSpeed 7.0, Ability: Arrow 50 Physical, Speed 55
- Tier 1: HP 175, MoveSpeed 7.0
- Tier 2: HP 200, MoveSpeed 7.0
- AnimationType: RangeAttack

**Apprentice (mage, Id: 1):**
- Tier 0: HP 60, MoveSpeed 3.5, Ability: Magic Arrow 1 (Magic)
- Tier 1: HP 80, MoveSpeed 3.7, EvolutionCost 150
- Tier 2: HP 100, MoveSpeed 4.0, EvolutionCost 300
- AnimationType: AbilityAttack

### Enemy юниты

**Goblin (melee, Id: 10):**
- Tier 0: HP 200, MoveSpeed 4.0, KillReward: 1

**Goblin Archer:**
- Tier 0: Range enemy, KillReward: 1

### Эволюция (текущая)

```
Noob → Warrior (30g) | Archer (45g) | Apprentice (100g)
Warrior → Knight (125g) | Paladin (175g) | Berserk (250g)
Apprentice → Fire Mage (115g) | Dark Mage (200g) | Heal Mage (300g)
```

### Экономика

```
StartingGold: 300
BaseArmySlots: 3
MaxArmySlots: 8
SlotUpgradeCost: 200
```

---

## Что нужно от тебя

### 1. Баланс юнитов игрока (11 юнитов)

Для каждого юнита заполни таблицу по 3 тирам:

```
| Юнит | Tier | HP | MoveSpeed | WeaponDmg | WeaponType | AbilityDmg | AbilityType | AbilityKind | PhysRes | MagRes | FireRes | FaithRes |
```

**Юниты для балансировки:**
1. Noob (стартовый, слабый, дешёвый)
2. Warrior (melee tank)
3. Knight (melee heavy tank)
4. Paladin (melee faith tank, faith resist)
5. Berserk (melee glass cannon)
6. Archer (ranged physical)
7. Apprentice (mage, magic projectile)
8. Fire Mage (mage, fire AoE)
9. Dark Mage (mage, magic projectile, высокий урон)
10. Heal Mage (support, faith AoE heal) — NOTE: лечение пока не реализовано, числа для будущего
11. Fish / Kaneki (unique heroes — сильнее обычных, не эволюционируют)

### 2. Враги (по уровням)

Спроектируй 5-7 типов врагов с ростом сложности:
- Уровень 1: простые враги (гоблины)
- Уровень 2-3: средние (орки, скелеты)
- Уровень 4-5: сложные (маги, боссы)

Для каждого врага: таблица как для юнитов + KillReward.

### 3. Дерево эволюции

```
Noob ──→ [ветка 1] ──→ [ветка 1a, 1b, 1c]
     ──→ [ветка 2] ──→ [ветка 2a, 2b, 2c]
     ──→ [ветка 3] ──→ [ветка 3a, 3b, 3c]
```

С ценами эволюции. Дорогие ветки = более специализированные юниты.

### 4. Уровни (5+ штук)

```
| Уровень | Название | KillsToComplete | Враг 1 (Tier, Count) | Враг 2 | Враг 3 | ... |
```

Count = одновременно на арене (респавн). Сложность нарастает.

### 5. Экономика

- StartingGold
- Цены юнитов (Price)
- SlotUpgradeCost
- Кривая доходов: сколько золота за прохождение уровня (KillReward * KillsToComplete)
- Цены эволюции
- Баланс: игрок должен чувствовать прогрессию, но не получать всё сразу. ~3-5 прохождений уровня чтобы позволить следующую эволюцию.

---

## Принципы баланса

1. **Rock-Paper-Scissors:** Physical > Low-armor, Magic > High-armor, Fire > Groups, Faith > Undead
2. **Tier scaling:** +20-30% к основным статам за тир
3. **Trade-offs:** Танки медленные но живучие, маги стеклянные но AoE, лучники — золотая середина
4. **Экономическая прогрессия:** дешёвые юниты доступны сразу, дорогие — после фарма 3-5 уровней
5. **Резисты:** специализированные юниты имеют 1 высокий резист (25-40%), остальные низкие (5-15%)
6. **Враги зеркалят игрока:** у врагов те же архетипы, но проще (меньше разнообразие)

---

## Формат ответа

Выдай ответ в виде таблиц, которые можно напрямую перенести в Unity ScriptableObject-ассеты. Каждое число должно быть финальным — не формулой, а конкретным значением.
