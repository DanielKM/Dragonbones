using RTSEngine.Entities;

namespace RTSEngine.BuildingExtension
{
    [System.Serializable]
    public struct BuildingAmount
    {
        public string name;

        public CodeCategoryField codes;

        public int amount;
    }
}
