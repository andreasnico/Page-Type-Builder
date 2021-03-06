﻿using System;
using PageTypeBuilder.Abstractions;
using PageTypeBuilder.Discovery;
using PageTypeBuilder.Synchronization;
using PageTypeBuilder.Tests.Helpers;
using Xunit;

namespace PageTypeBuilder.Tests.PageTypePropertyUpdaterTests
{
    public class ConstructorTests
    {
        [Fact]
        public void Constructor_SetsPageTypePropertyDefinitionLocatorPropertyToInstanceOfPageTypePropertyDefinitionLocator()
        {
            Type expectedPageTypePropertyDefinitionLocatorType = typeof(PageTypePropertyDefinitionLocator);

            PageTypePropertyUpdater updater = PageTypePropertyUpdaterFactory.Create();

            Assert.NotNull(updater.PageTypePropertyDefinitionLocator);
            Assert.Equal<Type>(expectedPageTypePropertyDefinitionLocatorType, updater.PageTypePropertyDefinitionLocator.GetType());
        }

        [Fact]
        public void Constructor_SetsPageDefinitionFactoryPropertyToInstanceOfPageDefinitionFactory()
        {
            Type expectedPageDefinitionFactoryType = typeof(PageDefinitionFactory);

            PageTypePropertyUpdater updater = PageTypePropertyUpdaterFactory.Create();

            Assert.NotNull(updater.PageDefinitionFactory);
            Assert.Equal<Type>(expectedPageDefinitionFactoryType, updater.PageDefinitionFactory.GetType());
        }

        [Fact]
        public void Constructor_SetsPageDefinitionTypeFactoryPropertyToInstanceOfPageDefinitionTypeFactory()
        {
            Type expectedPageDefinitionTypeFactoryType = typeof(PageDefinitionTypeFactory);

            PageTypePropertyUpdater updater = PageTypePropertyUpdaterFactory.Create();

            Assert.NotNull(updater.PageDefinitionTypeFactory);
            Assert.Equal<Type>(expectedPageDefinitionTypeFactoryType, updater.PageDefinitionTypeFactory.GetType());
        }

        [Fact]
        public void Constructor_SetsPageDefinitionTypeMapperPropertyToInstanceOfPageDefinitionTypeMapper()
        {
            Type expectedType = typeof(PageDefinitionTypeMapper);

            PageTypePropertyUpdater updater = PageTypePropertyUpdaterFactory.Create();

            Assert.NotNull(updater.PageDefinitionTypeMapper);
            Assert.Equal<Type>(expectedType, updater.PageDefinitionTypeMapper.GetType());
        }
    }
}
