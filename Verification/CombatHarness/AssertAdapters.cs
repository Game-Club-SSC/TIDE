// Assertion adapters execute the repository's tests without pretending that
// the NUnit runner or Unity Test Framework is installed.
using System;
namespace NUnit.Framework
{
 [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute {}
 [AttributeUsage(AttributeTargets.Method)] public class SetUpAttribute : Attribute {}
 [AttributeUsage(AttributeTargets.Method)] public class TearDownAttribute : Attribute {}
 public static class Assert
 {
  public static void IsTrue(bool value,string message="") { if(!value) throw new Exception(message); }
  public static void IsFalse(bool value,string message="")=>IsTrue(!value,message);
  public static void IsNull(object value,string message="")=>IsTrue(value==null,message);
  public static void IsNotNull(object value,string message="")=>IsTrue(value!=null,message);
  public static void AreSame(object expected,object actual,string message="")=>IsTrue(ReferenceEquals(expected,actual),message);
  public static void AreEqual(object expected,object actual,string message="")=>IsTrue(Equals(expected,actual),message+$" Expected {expected}, actual {actual}");
  public static void AreEqual(double expected,double actual,double tolerance,string message="")=>IsTrue(Math.Abs(expected-actual)<=tolerance,message+$" Expected {expected}, actual {actual}");
  public static void Greater(double actual,double threshold,string message="")=>IsTrue(actual>threshold,message+$" {actual} <= {threshold}");
  public static void Less(double actual,double threshold,string message="")=>IsTrue(actual<threshold,message+$" {actual} >= {threshold}");
  public static void GreaterOrEqual(double actual,double threshold,string message="")=>IsTrue(actual>=threshold,message);
  public static void LessOrEqual(double actual,double threshold,string message="")=>IsTrue(actual<=threshold,message);
  public static void Throws<T>(Action action,string message="") where T:Exception { try {action();} catch(T) {return;} throw new Exception(message); }
  public static void That(double actual,EqualityConstraint constraint,string message="")=>AreEqual(constraint.expected,actual,constraint.tolerance,message);
 }
 public static class Is { public static EqualityConstraint EqualTo(double value)=>new EqualityConstraint(value); }
 public class EqualityConstraint { public double expected,tolerance;public EqualityConstraint(double value) {expected=value;}public EqualityConstraint Within(double value) {tolerance=value;return this;} }
}
