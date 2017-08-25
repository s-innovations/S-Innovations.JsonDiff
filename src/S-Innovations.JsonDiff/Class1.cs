using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SInnovations.JsonDiff
{
    public class Operation
    {
        [JsonProperty("op")]
        public string Op { get; set; }
        [JsonProperty("value")]
        public JToken Value { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("from")]
        public JToken From { get; set; }
    }

    public static class JsonDiffExtensions
    {
        public static IEnumerable<Operation> Diff(JToken left, JToken right)
        {
            if (left.Type != right.Type)
                yield return new Operation { Op = "replace", Value = right, Path = left.Path };
            else if (left is JObject leftObj && right is JObject rightObj)
            {
                var leftProps = leftObj.Properties().ToLookup(p => p.Name);
                var rightProps = rightObj.Properties().ToLookup(p => p.Name);

                foreach (var added in rightProps.Where(p => !leftProps.Contains(p.Key)))
                {
                    var addedProp = added.First();
                    yield return new Operation { Op = "add", Value = addedProp.Value, Path = addedProp.Path };
                }

                foreach (var removed in leftProps.Where(p => !rightProps.Contains(p.Key)))
                {
                    var remove = removed.First();
                    yield return new Operation { Op = "remove", Value = remove.Value, Path = remove.Path };
                }

                foreach (var updated in leftProps.Where(p => rightProps.Contains(p.Key)))
                {
                    foreach (var diff in Diff(updated.First().Value, rightProps[updated.Key].First().Value))
                    {
                        yield return diff;
                    }
                }

            }
            else
            {
                if (left != right)
                {
                    yield return new Operation { Op = "replace", Path = left.Path, Value = right, From = left };
                }

            }


        }
    }
}
