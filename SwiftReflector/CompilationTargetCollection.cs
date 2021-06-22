using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SwiftReflector {
        public class CompilationTargetCollection : IList<CompilationTarget> {
                List<CompilationTarget> compilationTargets = new List<CompilationTarget> ();

                public CompilationTargetCollection ()
                {
                }

                public CompilationTarget this [int index] { get => compilationTargets [index]; set => compilationTargets [index] = value; }

                public int Count => compilationTargets.Count;

                public bool IsReadOnly => false;

                void TargetMismatchCheck (CompilationTarget item)
                {
                        if (IsTargetMismatched (item)) {
                                // if IsTargetMismatched returns true, there must be a first item.
                                var first = this [0];
                                var feedback = $"{first.ManufacturerToString ()}, {first.OperatingSystemToString ()}, {first.EnvironmentToString ()}";
                                throw new NotSupportedException ($"Item {item} does not match collection ({feedback})");
                        }
                }

                public void Add (CompilationTarget item)
                {
                        TargetMismatchCheck (item);
                        // we're not using a HashSet here because this collection is probably going to contain
                        // 4 items max, so I'm not worried about the speed
                        if (Contains (item))
                                return;
                        compilationTargets.Add (item);
                }

                public void Clear ()
                {
                        compilationTargets.Clear ();
                }

                public bool Contains (CompilationTarget item)
                {
                        return compilationTargets.Contains (item);
                }

                public void CopyTo (CompilationTarget [] array, int arrayIndex)
                {
                        compilationTargets.CopyTo (array, arrayIndex);
                }

                public IEnumerator<CompilationTarget> GetEnumerator ()
                {
                        return compilationTargets.GetEnumerator ();
                }

                public int IndexOf (CompilationTarget item)
                {
                        return compilationTargets.IndexOf (item);
                }

                public void Insert (int index, CompilationTarget item)
                {
                        TargetMismatchCheck (item);
                        compilationTargets.Insert (index, item);
                }

                public bool Remove (CompilationTarget item)
                {
                        return compilationTargets.Remove (item);
                }

                public void RemoveAt (int index)
                {
                        compilationTargets.RemoveAt (index);
                }

                IEnumerator IEnumerable.GetEnumerator ()
                {
                        return GetEnumerator ();
                }

                bool IsTargetMismatched (CompilationTarget target)
                {
                        if (Count == 0)
                                return false;
                        var first = this [0];
                        return target.Environment != first.Environment ||
                                target.Manufacturer != first.Manufacturer ||
                                target.OperatingSystem != first.OperatingSystem ||
                                target.MinimumOSVersion != first.MinimumOSVersion;
                }

                CompilationTarget FirstOrFail {
                        get {
                                if (Count == 0)
                                        throw new NotSupportedException ("empty collection");
                                return this [0];
                        }
                }

                public PlatformName OperatingSystem { get => FirstOrFail.OperatingSystem; }
                public string OperatingSystemString { get => FirstOrFail.OperatingSystemToString (); }
                public TargetManufacturer Manufacturer { get => FirstOrFail.Manufacturer; }
                public TargetEnvironment Environment { get => FirstOrFail.Environment; }
                public Version MinimumOSVersion { get => FirstOrFail.MinimumOSVersion; }

                public void AddRange (IEnumerable<CompilationTarget> fileTargets)
                {
                        foreach (var elem in fileTargets)
                                Add (elem);
                }
        }
}
