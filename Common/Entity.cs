﻿/*************************************************************************
 * 
 * Hxj.Data
 * 
 * 2010-2-10
 * 
 * steven hu   
 *  
 * Support: http://www.cnblogs.com/huxj
 *   
 * 
 * Change History:
 * 
 * 
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Dos.ORM;
using Dos.ORM.Common;

namespace Dos.ORM
{
    /// <summary>
    /// 实体属性修改记录 （字段修改记录）
    /// </summary>
    [Serializable]
    public class ModifyField
    {
        private Field field;
        private object oldValue;
        private object newValue;

        /// <summary>
        /// 字段
        /// </summary>
        public Field Field { get { return field; } }

        /// <summary>
        /// 原始值
        /// </summary>
        public object OldValue { get { return oldValue; } set { oldValue = value; } }

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get { return newValue; } set { newValue = value; } }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public ModifyField(Field field, object oldValue, object newValue)
        {
            this.field = field;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }



    }

    /// <summary>
    /// 实体信息
    /// </summary>
    [Serializable]
    public class Entity : Attribute
    {

        /// <summary>
        /// 表名
        /// </summary>
        private string tableName;

        /// <summary>
        /// 是否
        /// </summary>
        private bool isAttached = true;

        /// <summary>
        /// 实体状态
        /// </summary>
        private EntityState entityState = EntityState.Unchanged;

        ///// <summary>
        ///// 参数计数器  2015-07-30
        ///// </summary>
        //public int paramCount = 0;
        /// <summary>
        /// 修改的字段集合
        /// </summary>
        private List<ModifyField> modifyFields = new List<ModifyField>();

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public Entity()
        {
            var af = this.GetType().GetCustomAttributesData()
                            .Where(d => d.Constructor.DeclaringType == typeof(Entity))
                            .Select(d => new AttributeFactory(d)).FirstOrDefault();
            if (af != null)
            {
                var afe = af.Create() as Entity;
                this.tableName = afe != null ? afe.GetTableName() : this.GetType().Name;
            }
            else
                this.tableName = this.GetType().Name;
            this.isAttached = true;
            //this.paramCount = 0;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tableName">表名</param>
        public Entity(string tableName)
        {
            this.tableName = tableName;
            this.isAttached = true;
            // this.paramCount = 0;
        }


        #endregion

        /// <summary>
        /// 将实体置为修改状态
        /// </summary>
        public void Attach()
        {
            this.isAttached = true;
        }
        /// <summary>
        /// 将实体所有字段置为修改状态
        /// </summary>
        public void AttachAll()
        {
            AttachAll(false);
        }
        /// <summary>
        /// 将实体所有字段置为修改状态
        /// </summary>
        /// <param name="ignoreNullOrEmpty">忽略null值字段与空字符串字段</param>
        public void AttachAll(bool ignoreNullOrEmpty)
        {
            var fs = this.GetFields();
            var values = this.GetValues();
            for (int i = 0; i < fs.Length; i++)
            {
                if (ignoreNullOrEmpty && (values[i] == null || string.IsNullOrEmpty(values[i].ToString())))
                {
                    continue;
                }
                if (this.modifyFields.Any(d => d.Field.FieldName == fs[i].FieldName))
                {
                    continue;
                }
                this.modifyFields.Add(new ModifyField(fs[i], values[i], values[i]));
            }
        }

        /// <summary>
        /// 将实体置为指定状态（仅对.Save()有效果）
        /// </summary>
        public void Attach(EntityState entityState)
        {
            this.entityState = entityState;
        }
        /// <summary>
        /// 获取实体状态
        /// </summary>
        public EntityState GetEntityState()
        {
            return this.entityState;
        }
        /// <summary>
        /// 1、恢复实体为默认状态。
        /// 2、标记实体为不做任何数据库操作（仅对.Save()有效果）
        /// </summary>
        public void DeAttach()
        {
            this.isAttached = false;
            this.entityState = EntityState.Unchanged;
        }






        /// <summary>
        /// 记录 字段修改  
        /// </summary>
        /// <param name="field"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public void OnPropertyValueChange(Field field, object oldValue, object newValue)
        {
            if (isAttached)
            {
                #region 取消这个判断 2015-07-22
                //if (null == oldValue && null == newValue)
                //return;
                #endregion

                #region 取消这个判断 2015-07-01
                //if (null != oldValue && null != newValue && newValue.Equals(oldValue))
                //{
                //    return;
                //    #region HXJ HACK

                //    Type t = newValue.GetType();
                //    if (t == typeof(int))
                //    {
                //        if ((int)newValue != 0)
                //            return;
                //    }
                //    else if (t == typeof(decimal))
                //    {
                //        if (default(decimal) != (decimal)newValue)
                //            return;
                //    }
                //    else if (t == typeof(long))
                //    {
                //        if (default(long) != (long)newValue)
                //            return;
                //    }
                //    else if (t == typeof(bool))
                //    {
                //        if (default(bool) != (bool)newValue)
                //            return;
                //    }
                //    else if (t == typeof(Single))
                //    {
                //        if (default(Single) != (Single)newValue)
                //            return;
                //    }                   
                //    else
                //    {
                //        return;
                //    }

                //    #endregion
                //}
                #endregion

                lock (modifyFields)
                {
                    bool ishave = false;

                    foreach (var mf in modifyFields.Where(mf => mf.Field.PropertyName.ToLower().Equals(field.PropertyName.ToLower())))
                    {
                        mf.NewValue = newValue;
                        ishave = true;
                        break;
                    }

                    if (!ishave)
                    {
                        var modifyField = new ModifyField(field, oldValue, newValue);
                        modifyFields.Add(modifyField);
                    }
                }

            }

        }


        /// <summary>
        /// 清除修改记录
        /// </summary>
        public void ClearModifyFields()
        {
            modifyFields.Clear();
        }
        //2015-08-10 将没有任何地方使用此方法
        /// <summary>
        /// Sets the property values.
        /// </summary>
        /// <param name="reader">The reader.</param>
        [Obsolete("此方法作废！实体类可以不再需要！")]
        public virtual void SetPropertyValues(IDataReader reader) { }

        //2015-08-10 将没有任何地方使用此方法
        /// <summary>
        /// Sets the property values.
        /// </summary>
        /// <param name="row">The row.</param>
        [Obsolete("此方法作废！实体类可以不再需要！")]
        public virtual void SetPropertyValues(DataRow row) { }

        /// <summary>
        /// GetFields
        /// </summary>
        /// <returns></returns>
        public virtual Field[] GetFields() { return null; }

        /// <summary>
        /// GetValues
        /// </summary>
        /// <returns></returns>
        public virtual object[] GetValues() { return null; }

        /// <summary>
        /// GetPrimaryKeyFields
        /// </summary>
        /// <returns></returns>
        public virtual Field[] GetPrimaryKeyFields() { return null; }


        /// <summary>
        /// 标识列
        /// </summary>
        public virtual Field GetIdentityField()
        {
            return null;
        }

        /// <summary>
        /// 标识列的名称（例如如Oracle中Sequence名称）
        /// </summary>
        /// <returns></returns>
        public virtual string GetSequence()
        {
            return null;
        }

        /// <summary>
        /// 返回修改记录
        /// </summary>
        public List<ModifyField> GetModifyFields()
        {
            return modifyFields;
        }


        /// <summary>
        /// 是否只读
        /// </summary>
        public virtual bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        public string GetTableName()
        {
            return tableName;
        }

    }
    public class AttributeFactory
    {
        public AttributeFactory(CustomAttributeData data)
        {
            this.Data = data;

            var ctorInvoker = new ConstructorInvoker(data.Constructor);
            var ctorArgs = data.ConstructorArguments.Select(a => a.Value).ToArray();
            this.m_attributeCreator = () => ctorInvoker.Invoke(ctorArgs);

            this.m_propertySetters = new List<Action<object>>();
            foreach (var arg in data.NamedArguments)
            {
                var property = (PropertyInfo)arg.MemberInfo;
                var propertyAccessor = new PropertyAccessor(property);
                var value = arg.TypedValue.Value;
                this.m_propertySetters.Add(o => propertyAccessor.SetValue(o, value));
            }
        }

        public CustomAttributeData Data { get; private set; }

        private Func<object> m_attributeCreator;
        private List<Action<object>> m_propertySetters;

        public Attribute Create()
        {
            var attribute = this.m_attributeCreator();

            foreach (var setter in this.m_propertySetters)
            {
                setter(attribute);
            }

            return (Attribute)attribute;
        }
    }
}
