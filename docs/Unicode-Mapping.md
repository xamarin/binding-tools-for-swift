## Unicode Mapping

As Swift allows unicode identifiers in locations that are illegal in C#, there needs to be a mapping betwee the two. 

By default identifiers such as `UD83CUDF4E` are generated, based upon the unicode code points, but those are less than ideal to use.

By passing the `--unicode-mapping` with an XML file path, custom mappings can be provided.

### XML File Format

```
<?xml version=""1.0"" encoding=""utf-8""?>
<unicodemapping version = ""1.0"">
    <map from=""🍎"" to=""Apple""/>
</unicodemapping>
```