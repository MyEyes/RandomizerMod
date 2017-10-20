# RandomizerMod

This is a mod for [Hollow Knight](http://hollowknight.com/) that randomizes Charms and Abilities.

## Dependencies

This mod depends on the Modding API by Seanpr and Firzen, which is a modified `Assembly-CSharp.dll`.
There is currently no public download link for the Modding API.
For now, check the pinned messages in the #modding channel in the Hollow Knight discord.

## Development setup

After installing the Modding API, open this solution in Visual Studio.
You will get many errors for missing assembly references.
Here's how to resolve them:

1. Right click the **RandomizerMod** project in the Solution Explorer.
2. Properties
3. Referenced Paths
4. Folder: `C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\`
5. Add Folder

Now if you open up RandomizerMod.cs, you should see no errors.
