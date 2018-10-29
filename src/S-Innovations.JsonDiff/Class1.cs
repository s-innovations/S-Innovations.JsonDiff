using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    public class JsonDiffExtensionsOptions
    {
        public bool SupportArray { get; set; } = false;

        public Func<JToken, string> ArrayHashFunction { get; set; } = c => c.ToString();
    }
    public static class JsonDiffExtensions
    {
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static IEnumerable<Operation> Diff(JToken left, JToken right, JsonDiffExtensionsOptions options = null)
        {
            options = options ?? new JsonDiffExtensionsOptions();

            if (left.Type != right.Type)
                yield return new Operation { Op = "replace", Value = right?.DeepClone(), Path = left.Path };
            else if (left is JObject leftObj && right is JObject rightObj)
            {
                if (leftObj.Properties().Any() || rightObj.Properties().Any())
                {

                    var leftProps = leftObj.Properties().ToLookup(p => p.Name);
                    var rightProps = rightObj.Properties().ToLookup(p => p.Name);

                    foreach (var added in rightProps.Where(p => !leftProps.Contains(p.Key)))
                    {
                        var addedProp = added.First();
                        yield return new Operation { Op = "add", Value = addedProp.Value?.DeepClone(), Path = addedProp.Path };
                    }

                    foreach (var removed in leftProps.Where(p => !rightProps.Contains(p.Key)))
                    {
                        var remove = removed.First();
                        yield return new Operation { Op = "remove", Value = remove.Value?.DeepClone(), Path = remove.Path };
                    }

                    List<Operation> temp = new List<Operation>();
                    var all = true; var any = false;
                    foreach (var updated in leftProps.Where(p => rightProps.Contains(p.Key)))
                    {
                        var hit = false;
                        foreach (var diff in Diff(updated.First().Value, rightProps[updated.Key].First().Value, options))
                        {
                            hit = true;
                            temp.Add(diff);
                            // yield return diff;
                        }
                        all = all && hit;
                        any = any || hit;


                    }
                    if (all && leftObj.Properties().Any())
                        yield return new Operation { From = left?.DeepClone(), Value = right?.DeepClone(), Path = left.Path, Op = "replace" };
                    else
                    {
                        foreach (var op in temp)
                            yield return op;
                    }
                }

            }else if (options.SupportArray && left is JArray leftArray && right is JArray rightArray)
            {
                var leftHashs = leftArray.Select(c =>CreateMD5( options.ArrayHashFunction( c) )).ToArray();
                var rightHashs = rightArray.Select(c => CreateMD5(options.ArrayHashFunction(c))).ToArray();
                for(var i =0; i < leftHashs.Length; i++)
                {
                    var leftHash = leftHashs[i];
                    var rightIndex = Array.IndexOf(rightHashs, leftHash);
                    if (rightIndex == i)
                    {
                        //Nothing
                    }
                    else
                    {
                        if (rightIndex > -1)
                        {
                            yield return new Operation { Op = "move", From = leftArray[i].Path, Path = rightArray[rightIndex].Path };
                        }
                        else
                        {
                            

                            if (i < rightArray.Count)
                            {
                                foreach (var diff in Diff(leftArray[i], rightArray[i], options))
                                {
                                    yield return diff;
                                }
                            }
                            else
                            {
                                yield return new Operation { Op = "remove", Value = leftArray[i].DeepClone(), Path = leftArray[i].Path };
                            }

                        }

                    }
                }



                if (leftHashs.Length < rightHashs.Length)
                {
                    for (var j = leftHashs.Length; j < rightHashs.Length; j++)
                    {
                        yield return new Operation { Op = "add", Path = rightArray[j].Path, Value = rightArray[j].DeepClone() };
                    }
                }

              
               
            }
            else
            {

                if (!JToken.DeepEquals(left,right))
                {
                    yield return new Operation { Op = "replace", Path = left.Path, Value = right?.ToString(), From = left?.ToString() };
                }
              
                

            }


        }
    }
}
