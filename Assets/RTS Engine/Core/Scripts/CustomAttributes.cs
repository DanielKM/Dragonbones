using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine
{
    public class ReadOnlyAttribute : PropertyAttribute { }

    public class EntityCodeInputAttribute : PropertyAttribute { }

    public class EntityCategoryInputAttribute : PropertyAttribute {
        public bool IsDefiner { private set; get; }
        public EntityCategoryInputAttribute(bool isDefiner)
        {
            IsDefiner = isDefiner;
        }
    }

    public class EntityComponentCodeAttribute : PropertyAttribute {

        public bool TargetEntity { private set; get; } = false;
        public bool StartFromParentPath { private set; get; } = false;
        public string EntityPath { private set; get; }

        public EntityComponentCodeAttribute()
        {
            TargetEntity = false;
        }

        public EntityComponentCodeAttribute(string entityPath, bool startFromParentPath = false)
        {
            this.EntityPath = entityPath;
            this.StartFromParentPath = startFromParentPath;

            TargetEntity = true;
        }
    }

    public class EnforceTypeAttribute : PropertyAttribute
    {
        public IEnumerable<System.Type> EnforcedTypes { private set; get; }

        public bool PrefabOnly { private set; get; }
        public bool SameScene { private set; get; }
        public bool Child { private set; get; }

        public EnforceTypeAttribute(bool sameScene = false, bool prefabOnly = false)
            : this(
                new System.Type[0],
                sameScene,
                prefabOnly)
        { }

        public EnforceTypeAttribute(System.Type enforcedType, bool sameScene = false, bool prefabOnly = false)
            : this(
                new System.Type[] { enforcedType },
                sameScene,
                prefabOnly)
        { }

        public EnforceTypeAttribute(System.Type[] enforcedTypes, bool sameScene = false, bool prefabOnly = false)
        {
            this.EnforcedTypes = enforcedTypes;

            this.SameScene = sameScene;
            this.PrefabOnly = prefabOnly;
        }
    }
}
