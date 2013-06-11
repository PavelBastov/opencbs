﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:1.9.0.77
//      SpecFlow Generator Version:1.9.0.0
//      Runtime Version:4.0.30319.17929
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace OpenCBS.Specflow.Test
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "1.9.0.77")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("FLAT")]
    public partial class FLATFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "FLAT.feature"
#line hidden
        
        [NUnit.Framework.TestFixtureSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "FLAT", "", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.TestFixtureTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioSetup(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioStart(scenarioInfo);
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Flat schedule")]
        public virtual void FlatSchedule()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Flat schedule", ((string[])(null)));
#line 3
this.ScenarioSetup(scenarioInfo);
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "Name",
                        "Value"});
            table1.AddRow(new string[] {
                        "Type",
                        "Flat"});
            table1.AddRow(new string[] {
                        "Payments",
                        "Monthly (30 day)"});
            table1.AddRow(new string[] {
                        "Year",
                        "360"});
            table1.AddRow(new string[] {
                        "Rounding",
                        "Whole"});
            table1.AddRow(new string[] {
                        "Adjustment",
                        "Last installment"});
            table1.AddRow(new string[] {
                        "Date shift",
                        "Forward"});
            table1.AddRow(new string[] {
                        "Installments",
                        "5"});
            table1.AddRow(new string[] {
                        "Grace period",
                        "0"});
            table1.AddRow(new string[] {
                        "Amount",
                        "5000"});
            table1.AddRow(new string[] {
                        "Interest rate",
                        "24"});
            table1.AddRow(new string[] {
                        "Start on",
                        "01.01.2013"});
            table1.AddRow(new string[] {
                        "First repayment on",
                        "01.02.2013"});
#line 4
    testRunner.Given("this configuration", ((string)(null)), table1, "Given ");
#line 18
    testRunner.When("I create a schedule", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "Number",
                        "Start date",
                        "End date",
                        "Repayment date",
                        "Principal",
                        "Interest",
                        "Olb"});
            table2.AddRow(new string[] {
                        "1",
                        "01.01.2013",
                        "01.02.2013",
                        "01.02.2013",
                        "1000",
                        "100",
                        "5000"});
            table2.AddRow(new string[] {
                        "2",
                        "01.02.2013",
                        "01.03.2013",
                        "01.03.2013",
                        "1000",
                        "100",
                        "4000"});
            table2.AddRow(new string[] {
                        "3",
                        "01.03.2013",
                        "01.04.2013",
                        "01.04.2013",
                        "1000",
                        "100",
                        "3000"});
            table2.AddRow(new string[] {
                        "4",
                        "01.04.2013",
                        "01.05.2013",
                        "01.05.2013",
                        "1000",
                        "100",
                        "2000"});
            table2.AddRow(new string[] {
                        "5",
                        "01.05.2013",
                        "01.06.2013",
                        "03.06.2013",
                        "1000",
                        "100",
                        "1000"});
#line 19
    testRunner.Then("the schedule is", ((string)(null)), table2, "Then ");
#line hidden
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
