using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

    // The class is editor helper class
    //=========================================================================
    [ExecuteInEditMode]
    public class UIDataBinder : MonoBehaviour
    {
        #region Public Property
        //---------------------------------------------------------------------
        public UIDataSource dataSource = null;
        public UIDataRefreshRate refreshRate = UIDataRefreshRate.WindowInitialize;

        //---------------------------------------------------------------------
        public enum BindDirection { Forward, Reverse }
        [SerializeField, HideInInspector]
        public BindDirection bindDirection = BindDirection.Forward;

        //---------------------------------------------------------------------
        [SerializeField, HideInInspector]
        public string leftMemberName = string.Empty;

        //---------------------------------------------------------------------
        [SerializeField, HideInInspector]
        public string rightMemberName = string.Empty;

        //---------------------------------------------------------------------
        [SerializeField, HideInInspector]
        public List<string> leftMemberNames = new List<string>();

        //---------------------------------------------------------------------
        [SerializeField, HideInInspector]
        public List<string> rightMemberNames = new List<string>();

        //---------------------------------------------------------------------
        public bool namesChanged
        {
            get;
            private set;
        }
        #endregion

        #region Public Method
        //---------------------------------------------------------------------
        public UnityEngine.Object Refresh()
        {
            if (m_LeftMemberInfo == null || m_RightMemberInfo == null)
            {
                DebugHelper.LogWarning("Refresh data failed. " +
                    "target or src member info is null.", gameObject);
                return null;
            }

            try
            {
                object srcDataValue = null;
                try
                {
                    if (m_RightMemberInfo is FieldInfo)
                    {
                        FieldInfo fieldInfo = m_RightMemberInfo as FieldInfo;
                        srcDataValue = fieldInfo.GetValue(m_RightMemberObject);
                    }
                    else if (m_RightMemberInfo is PropertyInfo)
                    {
                        PropertyInfo propertyInfo = m_RightMemberInfo as PropertyInfo;
                        srcDataValue = propertyInfo.GetValue(m_RightMemberObject, null);
                    }
                }
                catch (Exception ex)
                {
                    DebugHelper.LogError("Get src field '" +
                        rightMemberName + "' failed.\r\n" + ex.Message, gameObject);
                }

                ApplyValueToLeft(srcDataValue);

                return m_LeftMemberObject;
            }
            catch (System.Exception ex)
            {
                DebugHelper.LogError("Set target field '" +
                    leftMemberName + "' failed.\r\n" + ex.Message, gameObject);
            }

            return null;
        }

        //---------------------------------------------------------------------
        [ContextMenu("Refresh")]
        public void RefreshInMenu()
        {
            CollectLeftMembers();
            RefreshRightMembers();
            namesChanged = true;
        }

        //---------------------------------------------------------------------
        public void CollectLeftMembers()
        {
            namesChanged = false;
            leftMemberNames.Clear();
            leftMemberNames.Add(noneName);
            leftMemberNames.Add(goActiveName);

            if (leftDataSource == null)
            {
                return;
            }

            Component ldsComponet = leftDataSource as Component;
            if (ldsComponet == null)
            {
                return;
            }

            Component[] components = ldsComponet.GetComponents<Component>();
            for (int index = 0; index < components.Length; ++index)
            {
                Type componentType = components[index].GetType();
                ForeachTypeMemberInfo(componentType, delegate(MemberInfo memberInfo)
                {
                    Type memberType = GetMemberType(memberInfo, true, false);
                    if (memberType != null)
                    {
                        leftMemberNames.Add(componentType.Name + "." + memberInfo.Name);
                    }
                });
            }
        }

        //---------------------------------------------------------------------
        public void RefreshRightMembers()
        {
            namesChanged = false;
            rightMemberNames.Clear();
            rightMemberNames.Add(noneName);
            rightMemberNames.Add(goActiveName);

            string memberName = string.Empty;
            UnityEngine.Object leftComponent = GetComponentByFullName(
                leftDataSource, leftMemberName, out memberName);
            MemberInfo leftMemberInfo =
                GetMemberInfo(leftComponent, memberName);
            if (leftMemberInfo == null)
            {
                return;
            }

            Type leftMemberType = GetMemberType(leftMemberInfo);
            Component rdsComponent = rightDataSource as Component;
            if (rdsComponent == null)
            {
                return;
            }

            Component[] components = rdsComponent.GetComponents<Component>();
            for (int index = 0; index < components.Length; ++index)
            {
                Type componentType = components[index].GetType();
                ForeachTypeMemberInfo(componentType, delegate(MemberInfo memberInfo)
                {
                    Type memberType = GetMemberType(memberInfo, false, true);
                    if (memberType != null && memberType.FullName == leftMemberType.FullName)
                    {
                        rightMemberNames.Add(componentType.Name + "." + memberInfo.Name);
                    }
                });
            }
        }

        //---------------------------------------------------------------------
        public bool RetrieveMemberInfo()
        {
            if (leftMemberName == noneName || rightMemberName == noneName)
            {
                return false;
            }

            string leftRealName = string.Empty;
            m_LeftMemberObject = GetComponentByFullName(
                leftDataSource, leftMemberName, out leftRealName);
            if (m_LeftMemberObject == null)
            {
                DebugHelper.LogError("Can not retrieve right data object.", gameObject);
                return false;
            }

            m_LeftMemberInfo = GetMemberInfo(m_LeftMemberObject, leftRealName);
            if (m_LeftMemberInfo == null)
            {
                DebugHelper.LogError("Can not get left member info '" + leftRealName +
                    "' form '" + m_LeftMemberObject.GetType().Name + "'", gameObject);
                return false;
            }

            string rightRealName = string.Empty;
            m_RightMemberObject = GetComponentByFullName(
                rightDataSource, rightMemberName, out rightRealName);
            if (m_RightMemberObject == null)
            {
                DebugHelper.LogError("Can not retrieve right data object.", gameObject);
                return false;
            }

            m_RightMemberInfo = GetMemberInfo(m_RightMemberObject, rightRealName);
            if (m_RightMemberInfo == null)
            {
                DebugHelper.LogError("Can not get right member info '" + rightRealName +
                    "' form '" + m_RightMemberObject.GetType().Name + "'", gameObject);
                return false;
            }

            return true;
        }
        #endregion

        #region Unity Method
        //---------------------------------------------------------------------
        private void Awake()
        {
            if (dataSource == null)
            {
                return;
            }

            if (Application.isPlaying &&
                refreshRate == UIDataRefreshRate.OnlyEditor)
            {
                return;
            }

            RetrieveMemberInfo();
            dataSource.AddDataBinder(this);
        }

        //---------------------------------------------------------------------
        private void Update()
        {
            if (refreshRate == UIDataRefreshRate.EachFrame)
            {
                Refresh();
            }
        }

        //---------------------------------------------------------------------
        private void OnEnable()
        {
            if (rightMemberName == goActiveName)
            {
                try
                {
                    ApplyValueToLeft(true);
                }
                catch (System.Exception ex)
                {
                    DebugHelper.LogError("Set target field '" +
                        leftMemberName + "' failed.\r\n" + ex.Message, gameObject);
                }
            }
        }

        //---------------------------------------------------------------------
        private void OnDisable()
        {
            if (rightMemberName == goActiveName)
            {
                try
                {
                    ApplyValueToLeft(false);
                }
                catch (System.Exception ex)
                {
                    DebugHelper.LogError("Set target field '" +
                        leftMemberName + "' failed.\r\n" + ex.Message, gameObject);
                }
            }
        }

        //---------------------------------------------------------------------
        private void OnDestroy()
        {
            if (dataSource != null)
            {
                dataSource.RemoveDataBinder(this);
            }
            dataSource = null;
            m_LeftMemberObject = null;
            m_RightMemberObject = null;
        }
        #endregion

        #region Internal Property
        //---------------------------------------------------------------------
        private UnityEngine.Object leftDataSource
        {
            get
            {
                if (bindDirection == BindDirection.Forward)
                {
                    return dataSource;
                }

                return this;
            }
        }

        //---------------------------------------------------------------------
        private UnityEngine.Object rightDataSource
        {
            get
            {
                if (bindDirection == BindDirection.Forward)
                {
                    return this;
                }

                return dataSource;
            }
        }
        #endregion

        #region Internal Method
        //---------------------------------------------------------------------
        private void ApplyValueToLeft(object value)
        {
            if (m_LeftMemberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = m_LeftMemberInfo as FieldInfo;
                fieldInfo.SetValue(m_LeftMemberObject, value);
            }
            else if (m_LeftMemberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = m_LeftMemberInfo as PropertyInfo;
                propertyInfo.SetValue(m_LeftMemberObject, value, null);
            }
        }

        //---------------------------------------------------------------------
        private Type GetMemberType(MemberInfo memberInfo)
        {
            return GetMemberType(memberInfo, false, false);
        }

        //---------------------------------------------------------------------
        private Type GetMemberType(MemberInfo memberInfo, bool checkWrite, bool checkRead)
        {
            Type memberType = null;
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                memberType = fieldInfo.FieldType;
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;

                if (checkRead)
                {
                    if (propertyInfo.CanRead)
                    {
                        memberType = propertyInfo.PropertyType;
                    }
                }
                else if (checkWrite)
                {
                    if (propertyInfo.CanWrite)
                    {
                        memberType = propertyInfo.PropertyType;
                    }
                }
                else
                {
                    memberType = propertyInfo.PropertyType;
                }
            }

            return memberType;
        }

        //---------------------------------------------------------------------
        private UnityEngine.Object GetComponentByFullName(
            UnityEngine.Object dsComponent, string fullName)
        {
            string memberName = string.Empty;
            return GetComponentByFullName(dsComponent, fullName, out memberName);
        }

        //---------------------------------------------------------------------
        private UnityEngine.Object GetComponentByFullName(
            UnityEngine.Object dsObj, string fullName, out string memberName)
        {
            Component dsComponent = dsObj as Component;
            string[] names = fullName.Split('.');
            if (dsComponent == null || names.Length != 2)
            {
                memberName = string.Empty;
                return null;
            }

            if (fullName == goActiveName)
            {
                memberName = names[1];
                return dsComponent.gameObject;
            }

            string srcComName = names[0];
            string srcMemName = names[1];
            memberName = srcMemName;

            Component[] components = dsComponent.GetComponents<Component>();
            for (int index = 0; index < components.Length; ++index)
            {
                Component component = components[index];
                Type comType = component.GetType();
                if (StringHelper.Equals(comType.Name, srcComName))
                {
                    return component;
                }
            }

            return null;
        }

        //---------------------------------------------------------------------
        private MemberInfo GetMemberInfo(UnityEngine.Object obj, string memberName)
        {
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();
            MemberInfo[] memberInfos =
                type.GetMember(memberName,
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Public);

            if (memberInfos == null ||
                memberInfos.Length == 0)
            {
                return null;
            }

            return memberInfos[0];
        }

        //---------------------------------------------------------------------
        private void ForeachTypeMemberInfo(Type type, System.Action<MemberInfo> handler)
        {
            if (type == GetType())
            {
                return;
            }

            MemberInfo[] memberInfos = type.GetMembers(
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Public);
            for (int i = 0; i < memberInfos.Length; ++i)
            {
                MemberInfo memberInfo = memberInfos[i];
                if (memberInfo.MemberType != MemberTypes.Field &&
                    memberInfo.MemberType != MemberTypes.Property)
                {
                    continue;
                }

                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    FieldInfo fieldInfo = memberInfo as FieldInfo;
                    if (!fieldInfo.IsPublic)
                    {
                        object[] customAttributes = memberInfo.
                            GetCustomAttributes(typeof(SerializeField), false);
                        if (customAttributes == null || customAttributes.Length == 0)
                        {
                            continue;
                        }
                    }
                }

                if (memberInfo.Name == "m_WindowGroup" ||
                    memberInfo.Name == "windowGroup")
                {
                    continue;
                }

                handler(memberInfo);
            }
        }
        #endregion

        #region Internal Member
        //---------------------------------------------------------------------
        public const string noneName = "None";
        public const string goActiveName = "gameObject.active";

        //---------------------------------------------------------------------
        private MemberInfo m_LeftMemberInfo = null;
        private UnityEngine.Object m_LeftMemberObject = null;
        private MemberInfo m_RightMemberInfo = null;
        private UnityEngine.Object m_RightMemberObject = null;
        #endregion
    }
