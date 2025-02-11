﻿using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Gherkin.Quick;

namespace UnitTests
{
    public sealed class FeatureDiscoveryModel_Discover_Should
    {
        private readonly Mock<IFeatureFileRepository> _featureFileRepository = new Mock<IFeatureFileRepository>();
        private readonly FeatureDiscoveryModel _sut;

        public FeatureDiscoveryModel_Discover_Should()
        {
            _sut = new FeatureDiscoveryModel(_featureFileRepository.Object);
        }

        [Fact]
        public void Require_FeatureClassType()
        {
            //act / assert.
            Assert.Throws<ArgumentNullException>(() => _sut.Discover(null));
        }

        [Theory]
        [InlineData(typeof(MyFeature), "MyFeature.feature")]
        [InlineData(typeof(MyFeatureWithAttribute), "/my/path/to/feature/file.feature")]
        public void Throw_When_Feature_File_Not_Found(
            Type featureClassType,
            string fileName)
        {
            //arrange.
            _featureFileRepository.Setup(r => r.GetByFilePath(fileName))
                .Returns<FeatureFile>(null)
                .Verifiable();

            //act / assert.
            Assert.Throws<System.IO.FileNotFoundException>(() => _sut.Discover(featureClassType));

            //assert.
            _featureFileRepository.Verify();
        }

        [Theory]
        [InlineData(typeof(MyFeature), "MyFeature.feature")]
        [InlineData(typeof(MyFeatureWithAttribute), "/my/path/to/feature/file.feature")]
        public void Find_Scenarios_In_Feature_File(
            Type featureClassType,
            string fileName)
        {
            //arrange.
            var gherkinFeature = new GherkinFeatureBuilder().WithScenario("scenario1", steps =>
                steps.Given("step 1", null))
                .Build();
            _featureFileRepository.Setup(r => r.GetByFilePath(fileName))
                .Returns(new FeatureFile(new Gherkin.Ast.GherkinDocument(gherkinFeature, null)))
                .Verifiable();

            //act.
            var features = _sut.Discover(featureClassType);
            var featureFile = features.First();

            //assert.
            _featureFileRepository.Verify();
            
            Assert.NotNull(featureFile.Feature);
            Assert.Same( gherkinFeature , featureFile.Feature);
        }

        [Theory]
        [InlineData(typeof(AddFeature), _addFeature_pattern, new string[] {"AddTwoNumbers.feature", "AddNumbersTo5.feature"})]
        [InlineData(typeof(ComplexFeature), _complexFeature_pattern, new string[] {"Features/ComplexGroupOfScenarios1.feature", "Features/ComplexGroupOfScenarios2.feature", "Features/ComplexGroupOfScenarios3.feature", "Features/Complex.feature"})]
        public void Find_Scenarios_In_Many_Feature_Files_Sharing_Pattern(
            Type featureClassType, string pattern, string[] files)
        {
            //arrange.

            _featureFileRepository.Setup(r => r.FindFilesByPattern(pattern) )
                .Returns( new List<String>(files))
                .Verifiable();

            int i = 0;
            files.ToList().ForEach( file => {
                    var gherkinFeature = new GherkinFeatureBuilder().WithScenario($"First scenario from Feature File {i+1}", steps =>
                        steps.Given("step 1", null))
                        .Build();

                    _featureFileRepository.Setup(r => r.GetByFilePath(file))
                        .Returns(new FeatureFile(new Gherkin.Ast.GherkinDocument(gherkinFeature, null)))
                        .Verifiable();
                    ++i;
                }
            );

            //act.
            var features = _sut.Discover(featureClassType);

            //assert.
            _featureFileRepository.Verify();

            i = 0;
            features.ToList().ForEach(feature => {
                Assert.NotNull(feature.Feature);
                Assert.Equal(files[i], feature.Path);
                // let's check scenarios are correct
                Assert.Equal($"First scenario from Feature File {i+1}", feature.Feature.Children.First().Name);
                ++i;
            });
        }

        private sealed class MyFeature : Feature
        {

        }

        [FeatureFile(path: "/my/path/to/feature/file.feature")]
        private sealed class MyFeatureWithAttribute : Feature
        {

        }

        private const string _addFeature_pattern = "Add*.feature";
        [FeatureFile(_addFeature_pattern)]
        private sealed class AddFeature : Feature
        {

        }

        private const string _complexFeature_pattern = "Features/Complex*.feature";
        [FeatureFile(_complexFeature_pattern)]
        private sealed class ComplexFeature : Feature
        {

        }

    }
}
