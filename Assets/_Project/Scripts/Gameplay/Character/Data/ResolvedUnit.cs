namespace _Project.Scripts.Gameplay.Character.Data
{
    public struct ResolvedUnit
    {
        public int InstanceId;
        public CharacterData Data;
        public int TierIndex;

        public TierData Tier => Data.GetTier(TierIndex);
    }
}
