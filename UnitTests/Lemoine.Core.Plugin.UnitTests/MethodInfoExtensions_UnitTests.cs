// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Lemoine.Core.Log;
using Lemoine.Conversion;

namespace Lemoine.Core.Plugin.UnitTests
{
  public enum EnumTest
  {
    First = 1,
    Second,
  }

  public class AlternativeAutoConverter
    : DefaultAutoConverter
  {
    public override bool IsCompatible (object x, Type t)
    {
      if (x.GetType () == typeof (string)) {
        // If the input data is a boolean, limit the type to a boolean
        if (t == typeof (bool)) {
          switch ((string)x) {
          case "True":
          case "False":
            return true;
          default:
            return false;
          }
        }

        // If the input data is a number, limit the type to be a number
        if (base.IsCompatible (x, typeof (decimal)) || base.IsCompatible (x, typeof (double))) {
          return t.IsNumeric () && base.IsCompatible (x, t);
        }
      }

      return base.IsCompatible (x, t);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class MethodInfoExtensions_UnitTests
  {
    readonly ILog log = LogManager.GetLogger (typeof (MethodInfoExtensions_UnitTests).FullName);

    /// <summary>
    /// Constructor
    /// </summary>
    public MethodInfoExtensions_UnitTests ()
    { }

    /// <summary>
    /// Test 
    /// </summary>
    [Test]
    public void TestInvokeAutoConvert ()
    {
      var autoConverter = new DefaultAutoConverter ();
      var methods = this.GetType ().GetMethods ();
      var matchingMethods = methods.Where (m => m.Name.Equals ("Test"))
        .Where (m => m.IsParameterMatch (autoConverter, "123", "1.23", "First", "s"));
      Assert.AreEqual (1, matchingMethods.Count ());
      var method = matchingMethods.First ();
      var result = method.InvokeAutoConvert (autoConverter, this, "123", "1.23", "First", "s");
      Assert.AreEqual (456, result);

      var matchingMethods2 = methods.Where (m => m.Name.Equals ("Test"))
        .Where (m => m.IsParameterMatch (autoConverter, "123"));
      Assert.IsFalse (matchingMethods2.Any ());

      var result2 = method.InvokeAutoConvert (autoConverter, this, (object)"123", (object)"1.23", (object)"First", (object)"s");
      Assert.AreEqual (456, result2);
    }

    /// <summary>
    /// Test 
    /// </summary>
    [Test]
    public void TestInvokeAlternativeAutoConvert ()
    {
      var autoConverter = new AlternativeAutoConverter ();
      var methods = this.GetType ().GetMethods ();
      var matchingMethods = methods.Where (m => m.Name.Equals ("Test"))
        .Where (m => m.IsParameterMatch (autoConverter, "123", "1.23", "First", "s"));
      Assert.AreEqual (1, matchingMethods.Count ());
      var method = matchingMethods.First ();
      var result = method.InvokeAutoConvert (autoConverter, this, "123", "1.23", "First", "s");
      Assert.AreEqual (456, result);

      var matchingMethods2 = methods.Where (m => m.Name.Equals ("Test"))
        .Where (m => m.IsParameterMatch (autoConverter, "123"));
      Assert.IsFalse (matchingMethods2.Any ());

      var result2 = method.InvokeAutoConvert (autoConverter, this, (object)"123", (object)"1.23", (object)"First", (object)"s");
      Assert.AreEqual (456, result2);

      var toolTypeResult = methods.Single (m => m.Name.Equals ("GetToolType"))
        .InvokeAutoConvert (autoConverter, this, "23");
      Assert.AreEqual ("TTT23", toolTypeResult);
    }

    public int Test (int x, double d, EnumTest enumTest, int y)
    {
      Assert.IsTrue (false);
      return 0;
    }
    public int Test (int x, double d, EnumTest enumTest, string s)
    {
      Assert.AreEqual (123, x);
      Assert.AreEqual (1.23, d);
      Assert.AreEqual (EnumTest.First, enumTest);
      Assert.AreEqual ("s", s);
      return 456;
    }

    public string GetToolType (int toolNo)
    {
      return $"TTT{toolNo}";
    }

  }
}
