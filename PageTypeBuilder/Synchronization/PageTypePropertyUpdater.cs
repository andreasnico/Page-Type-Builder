﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EPiServer.Core.PropertySettings;
using EPiServer.DataAbstraction;
using EPiServer.Editor;
using log4net;
using PageTypeBuilder.Abstractions;
using PageTypeBuilder.Discovery;

namespace PageTypeBuilder.Synchronization
{
    public class PageTypePropertyUpdater
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PageTypePropertyUpdater));
        private ITabFactory _tabFactory;
        private IPropertySettingsRepository _propertySettingsRepository;
        private IGlobalPropertySettingsLocator globalPropertySettingsLocator;

        public PageTypePropertyUpdater(
            IPageDefinitionFactory pageDefinitionFactory, 
            IPageDefinitionTypeFactory pageDefinitionTypeFactory, 
            ITabFactory tabFactory,
            IPropertySettingsRepository propertySettingsRepository,
            IGlobalPropertySettingsLocator globalPropertySettingsLocator)
        {
            PageDefinitionFactory = pageDefinitionFactory;
            PageDefinitionTypeFactory = pageDefinitionTypeFactory;
            PageTypePropertyDefinitionLocator = new PageTypePropertyDefinitionLocator();
            PageDefinitionTypeMapper = new PageDefinitionTypeMapper(PageDefinitionTypeFactory);
            _tabFactory = tabFactory;
            _propertySettingsRepository = propertySettingsRepository;
            this.globalPropertySettingsLocator = globalPropertySettingsLocator;
        }

        protected internal virtual void UpdatePageTypePropertyDefinitions(IPageType pageType, PageTypeDefinition pageTypeDefinition)
        {
            IEnumerable<PageTypePropertyDefinition> definitions = 
                PageTypePropertyDefinitionLocator.GetPageTypePropertyDefinitions(pageType, pageTypeDefinition.Type);

            foreach (PageTypePropertyDefinition propertyDefinition in definitions)
            {
                PageDefinition pageDefinition = GetExistingPageDefinition(pageType, propertyDefinition) ??
                                                CreateNewPageDefinition(propertyDefinition);

                UpdatePageDefinition(pageDefinition, propertyDefinition); 

                UpdatePropertySettings(pageTypeDefinition, propertyDefinition, pageDefinition);
            }
        }

        protected internal virtual void UpdatePropertySettings(PageTypeDefinition pageTypeDefinition, PageTypePropertyDefinition propertyDefinition, PageDefinition pageDefinition)
        {
            PropertySettingsContainer container = GetPropertySettingsContainer(pageDefinition);

            object[] attributes = GetPropertyAttributes(propertyDefinition, pageTypeDefinition);
            var useGlobalSettingsAttribute = attributes.OfType<UseGlobalSettingsAttribute>().FirstOrDefault();
            if(useGlobalSettingsAttribute != null)
            {
                //TODO: Should validate not null and valid type at startup
                var globalSettingsUpdater = globalPropertySettingsLocator.GetGlobalPropertySettingsUpdaters().Where(u => u.WrappedInstanceType == useGlobalSettingsAttribute.Type).First();
                var wrapper =_propertySettingsRepository.GetGlobals(globalSettingsUpdater.SettingsType)
                    .Where(w => globalSettingsUpdater.Match(w))
                    .First();
                container.Settings[globalSettingsUpdater.SettingsType.FullName] = wrapper;
                //TODO: Add spec validating that exception is thrown with the below uncommented (An item with the same key has already been added.)
                //container.Settings.Add(globalSettingsUpdater.SettingsType.FullName, wrapper);
                _propertySettingsRepository.Save(container);
            }

            List<PropertySettingsUpdater> settingsUpdaters = GetPropertySettingsUpdaters(pageTypeDefinition, propertyDefinition, pageDefinition);
            settingsUpdaters.ForEach(updater =>
                {
                    var wrapper = container.GetSetting(updater.SettingsType);
                    if (wrapper == null)
                    {
                        wrapper = new PropertySettingsWrapper();
                        container.Settings[updater.SettingsType.FullName] = wrapper;
                        //TODO: Add spec validating that exception is thrown with the below uncommented (An item with the same key has already been added.)
                        //container.Settings.Add(updater.SettingsType.FullName, wrapper);
                    }

                    bool settingsAlreadyExists = true;
                    if (wrapper.PropertySettings == null)
                    {
                        wrapper.PropertySettings = ((IPropertySettings)Activator.CreateInstance(updater.SettingsType)).GetDefaultValues();
                        settingsAlreadyExists = false;
                    }

                    if (settingsAlreadyExists && !updater.OverWriteExisting)
                        return;

                    int hashBeforeUpdate = updater.GetSettingsHashCode(wrapper.PropertySettings);
                    updater.UpdateSettings(wrapper.PropertySettings);
                    int hashAfterUpdate = updater.GetSettingsHashCode(wrapper.PropertySettings);
                    if (hashBeforeUpdate != hashAfterUpdate || !settingsAlreadyExists)
                    {
                        _propertySettingsRepository.Save(container);
                    }
                });
        }

        private PropertySettingsContainer GetPropertySettingsContainer(PageDefinition pageDefinition)
        {
            PropertySettingsContainer container;

            if (pageDefinition.SettingsID == Guid.Empty)
            {
                pageDefinition.SettingsID = Guid.NewGuid();
                PageDefinitionFactory.Save(pageDefinition);
                container = new PropertySettingsContainer(pageDefinition.SettingsID);
            }
            else
            {
                if (!_propertySettingsRepository.TryGetContainer(pageDefinition.SettingsID, out container))
                {
                    container = new PropertySettingsContainer(pageDefinition.SettingsID);
                }
            }
            return container;
        }

        private List<PropertySettingsUpdater> GetPropertySettingsUpdaters(PageTypeDefinition pageTypeDefinition, PageTypePropertyDefinition propertyDefinition, PageDefinition pageDefinition)
        {
            object[] attributes = GetPropertyAttributes(propertyDefinition, pageTypeDefinition);
            var settingsUpdaters = new List<PropertySettingsUpdater>();
            foreach (var attribute in attributes)
            {
                foreach (var interfaceType in attribute.GetType().GetInterfaces())
                {
                    if (!interfaceType.IsGenericType)
                        continue;

                    if(!typeof (IUpdatePropertySettings<>).IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                        continue;
                    var settingsType = interfaceType.GetGenericArguments().First();
                    var updater = new PropertySettingsUpdater(settingsType, attribute);
                    settingsUpdaters.Add(updater);
                }
            }
            return settingsUpdaters;
        }

        private object[] GetPropertyAttributes(PageTypePropertyDefinition propertyDefinition, PageTypeDefinition pageTypeDefinition)
        {
            PropertyInfo prop;

            if (propertyDefinition.Name.Contains("-"))
            {
                // the property definition is a property belonging to a property group
                int index = propertyDefinition.Name.IndexOf("-");
                string propertyGroupPropertyName = propertyDefinition.Name.Substring(0, index);
                string propertyName = propertyDefinition.Name.Substring(index + 1);

                PropertyInfo propertyGroupProperty = pageTypeDefinition.Type.GetProperties().Where(p => string.Equals(p.Name, propertyGroupPropertyName)).FirstOrDefault();
                prop = propertyGroupProperty.PropertyType.GetProperties().Where(p => string.Equals(p.Name, propertyName)).FirstOrDefault();
            }
            else
                prop = pageTypeDefinition.Type.GetProperties().Where(p => string.Equals(p.Name, propertyDefinition.Name)).FirstOrDefault();

            return prop.GetCustomAttributes(true);
        }

        protected internal virtual PageDefinition GetExistingPageDefinition(IPageType pageType, PageTypePropertyDefinition propertyDefinition)
        {
            return pageType.Definitions.FirstOrDefault(definition => definition.Name == propertyDefinition.Name);
        }

        protected internal virtual PageDefinition CreateNewPageDefinition(PageTypePropertyDefinition propertyDefinition)
        {
            PageDefinition pageDefinition = new PageDefinition();
            pageDefinition.PageTypeID = propertyDefinition.PageType.ID;
            pageDefinition.Name = propertyDefinition.Name;
            pageDefinition.EditCaption = propertyDefinition.GetEditCaptionOrName();
            SetPageDefinitionType(pageDefinition, propertyDefinition);

            PageDefinitionFactory.Save(pageDefinition);
            
            return pageDefinition;
        }

        protected internal virtual void SetPageDefinitionType(PageDefinition pageDefinition, PageTypePropertyDefinition propertyDefinition)
        {
            pageDefinition.Type = GetPageDefinitionType(propertyDefinition);
        }

        protected internal virtual PageDefinitionType GetPageDefinitionType(PageTypePropertyDefinition definition)
        {
            return PageDefinitionTypeMapper.GetPageDefinitionType(definition);
        }

        protected internal virtual void UpdatePageDefinition(PageDefinition pageDefinition, PageTypePropertyDefinition pageTypePropertyDefinition)
        {
            string oldValues = SerializeValues(pageDefinition);

            UpdatePageDefinitionValues(pageDefinition, pageTypePropertyDefinition);

            string updatedValues = SerializeValues(pageDefinition);
            if (updatedValues != oldValues)
            {
                log.Debug(string.Format("Updating PageDefintion, old values: {0}, new values: {1}.", oldValues, updatedValues));
                PageDefinitionFactory.Save(pageDefinition);
            }
        }

        protected internal virtual string SerializeValues(PageDefinition pageDefinition)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("EditCaption:");
            builder.Append(pageDefinition.EditCaption);
            builder.Append("|");
            builder.Append("HelpText:");
            builder.Append(pageDefinition.HelpText);
            builder.Append("|");
            builder.Append("Required:");
            builder.Append(pageDefinition.Required);
            builder.Append("|");
            builder.Append("Searchable:");
            builder.Append(pageDefinition.Searchable);
            builder.Append("|");
            builder.Append("DefaultValue:");
            builder.Append(pageDefinition.DefaultValue);
            builder.Append("|");
            builder.Append("DefaultValueType:");
            builder.Append(pageDefinition.DefaultValueType);
            builder.Append("|");
            builder.Append("LanguageSpecific:");
            builder.Append(pageDefinition.LanguageSpecific);
            builder.Append("|");
            builder.Append("DisplayEditUI:");
            builder.Append(pageDefinition.DisplayEditUI);
            builder.Append("|");
            builder.Append("FieldOrder:");
            builder.Append(pageDefinition.FieldOrder);
            builder.Append("|");
            builder.Append("LongStringSettings:");
            builder.Append(pageDefinition.LongStringSettings);
            builder.Append("|");
            builder.Append("Tab.ID:");
            builder.Append(pageDefinition.Tab.ID);
            builder.Append("|"); 

            return builder.ToString();
        }

        protected internal virtual void UpdatePageDefinitionValues(PageDefinition pageDefinition, PageTypePropertyDefinition pageTypePropertyDefinition)
        {
            PageTypePropertyAttribute propertyAttribute = pageTypePropertyDefinition.PageTypePropertyAttribute;

            pageDefinition.EditCaption = pageTypePropertyDefinition.GetEditCaptionOrName();
            pageDefinition.HelpText = propertyAttribute.HelpText ?? string.Empty;
            pageDefinition.Required = propertyAttribute.Required;
            pageDefinition.Searchable = propertyAttribute.Searchable;
            pageDefinition.DefaultValue = propertyAttribute.DefaultValue != null ? propertyAttribute.DefaultValue.ToString() : string.Empty;
            pageDefinition.DefaultValueType = propertyAttribute.DefaultValueType;
            pageDefinition.LanguageSpecific = propertyAttribute.UniqueValuePerLanguage;
            pageDefinition.DisplayEditUI = propertyAttribute.DisplayInEditMode;
            pageDefinition.FieldOrder = GetFieldOrder(pageDefinition, propertyAttribute);
            UpdatePageDefinitionTab(pageDefinition, propertyAttribute);
        }

        private int GetFieldOrder(PageDefinition pageDefinition, PageTypePropertyAttribute propertyAttribute)
        {
            int fieldOrder = propertyAttribute.SortOrder;
            if(fieldOrder == PageTypePropertyAttribute.SortOrderNoValue)
            {
                fieldOrder = 0;
                if(pageDefinition.FieldOrder != 0)
                {
                    fieldOrder = pageDefinition.FieldOrder;
                }
            }
            return fieldOrder;
        }

        protected internal virtual void UpdatePageDefinitionTab(PageDefinition pageDefinition, PageTypePropertyAttribute propertyAttribute)
        {
            TabDefinition tab = _tabFactory.List().First();
            if (propertyAttribute.Tab != null)
            {
                Tab definedTab = (Tab) Activator.CreateInstance(propertyAttribute.Tab);
                tab = _tabFactory.GetTabDefinition(definedTab.Name);
            }
            pageDefinition.Tab = tab;
        }

        internal PageTypePropertyDefinitionLocator PageTypePropertyDefinitionLocator { get; set; }

        internal IPageDefinitionFactory PageDefinitionFactory { get; set; }

        internal IPageDefinitionTypeFactory PageDefinitionTypeFactory { get; set; }

        internal PageDefinitionTypeMapper PageDefinitionTypeMapper { get; set; }
    }
}