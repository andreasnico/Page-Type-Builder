﻿using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.Editor;
using EPiServer.Security;
using Machine.Specifications;
using PageTypeBuilder.Specs.Helpers;
using PageTypeBuilder.Specs.Helpers.TypeBuildingDsl;

namespace PageTypeBuilder.Specs.Synchronization.PageTypeSynchronization.PropertySynchronization.ValueSetting
{
    [Subject("Synchronization")]
    public class when_a_new_property_with_PageTypePropertyAttribute_has_been_added_to_a_page_type_class
        : SynchronizationSpecs
    {
        static string propertyName = "PropertyName";
        static PageTypePropertyAttribute propertyAttribute;

        Establish context = () =>
        {
            propertyAttribute = new PageTypePropertyAttribute();
            propertyAttribute.DefaultValue = "Specified default value";
            propertyAttribute.DefaultValueType = DefaultValueType.Value;
            propertyAttribute.DisplayInEditMode = false;
            propertyAttribute.EditCaption = "Property's Edit Caption";
            propertyAttribute.HelpText = "Property's help text";
            propertyAttribute.Required = true;
            propertyAttribute.Searchable = true;
            propertyAttribute.SortOrder = 123;
            propertyAttribute.UniqueValuePerLanguage = true;

            SyncContext.CreateAndAddPageTypeClassToAppDomain(type =>
                type.AddProperty(prop =>
                {
                    prop.Name = propertyName;
                    prop.Type = typeof(string);
                    prop.AddAttributeTemplate(propertyAttribute);
                }));
        };

        Because of =
            () => SyncContext.PageTypeSynchronizer.SynchronizePageTypes();

        It should_create_a_new_PageDefinition =
            () => SyncContext.PageDefinitionFactory.List().ShouldNotBeEmpty();

        It should_create_a_PageDefinition_whose_PageTypeID_is_equal_to_the_page_types_ID =
            () => SyncContext.PageDefinitionFactory.List().First().PageTypeID
                .ShouldEqual(SyncContext.PageTypeFactory.List().First().ID);

        It should_create_a_PageDefinition_whose_name_equals_the_propertys_name =
            () => SyncContext.PageDefinitionFactory.List().First().Name.ShouldEqual(propertyName);

        It should_create_a_PageDefinition_whose_HelpText_equals_the_attributes_HelpText =
            () => SyncContext.PageDefinitionFactory.List().First()
                .HelpText.ShouldEqual(propertyAttribute.HelpText);

        It should_create_a_PageDefinition_whose_EditCaption_equals_the_attributes_EditCaption =
            () => SyncContext.PageDefinitionFactory.List().First().EditCaption.ShouldEqual(propertyAttribute.EditCaption);

        It should_create_a_PageDefinition_whose_FieldOrder_equals_the_attributes_SortOrder =
            () => SyncContext.PageDefinitionFactory.List().First().FieldOrder.ShouldEqual(propertyAttribute.SortOrder);

        It should_create_a_PageDefinition_whose_DefaultValue_equals_the_attributes_DefaultValue =
            () => SyncContext.PageDefinitionFactory.List().First()
                .DefaultValue.ShouldEqual(propertyAttribute.DefaultValue);

        It should_create_a_PageDefinition_whose_DefaultValueType_equals_the_attributes_DefaultValueType =
            () => SyncContext.PageDefinitionFactory.List().First()
                .DefaultValueType.ShouldEqual(propertyAttribute.DefaultValueType);

        It should_create_a_PageDefinition_whose_DisplayEditUI_equals_the_attributes_DisplayInEditMode =
            () => SyncContext.PageDefinitionFactory.List().First()
                .DisplayEditUI.ShouldEqual(propertyAttribute.DisplayInEditMode);

        It should_create_a_PageDefinition_whose_Required_property_equals_the_attributes_Required_property =
            () => SyncContext.PageDefinitionFactory.List().First()
                .Required.ShouldEqual(propertyAttribute.Required);

        It should_create_a_PageDefinition_whose_Searchable_property_equals_the_attributes_Searchable_property =
            () => SyncContext.PageDefinitionFactory.List().First()
                .Searchable.ShouldEqual(propertyAttribute.Searchable);

        It should_create_a_PageDefinition_whose_LanguageSpecific_property_equals_the_attributes_UniqueValuePerLanguage_property =
            () => SyncContext.PageDefinitionFactory.List().First()
                .LanguageSpecific.ShouldEqual(propertyAttribute.UniqueValuePerLanguage);
    }

    [Subject("Synchronization")]
    public class when_a_page_type_class_has_a_property_with_a_tab_specified
        : SynchronizationSpecs
    {
        static string propertyName = "PropertyName";
        static string tabName = "NameOfTheTab";

        Establish context = () =>
        {
            var tabClass = TabClassFactory.CreateTabClass(
            "NameOfClass", tabName, AccessLevel.Undefined, 0);

            SyncContext.AssemblyLocator.Add(tabClass.Assembly);
            PageTypePropertyAttribute propertyAttribute;
            propertyAttribute = new PageTypePropertyAttribute();
            propertyAttribute.Tab = tabClass;

            SyncContext.CreateAndAddPageTypeClassToAppDomain(type =>
                type.AddProperty(prop =>
                {
                    prop.Name = propertyName;
                    prop.Type = typeof(string);
                    prop.AddAttributeTemplate(propertyAttribute);
                }));
        };

        Because of =
            () => SyncContext.PageTypeSynchronizer.SynchronizePageTypes();

        It should_create_a_page_definition_whose_Tab_property_matches_the_specified_Tab =
            () => SyncContext.PageDefinitionFactory.List().First().Tab.ID.ShouldEqual(SyncContext.TabFactory.GetTabDefinition(tabName).ID);
    }
}
