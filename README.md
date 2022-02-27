# Dotnet Patcher

This repo contains the code to build and test the `dotnet-patcher` tool.

## Motivations

I like modding games, and many games are now using .Net as a scripting language.
It can a home-made solution or just because the game is developped using the
[UnityEngine](https://unity.com). I'm also a Linux user. I don't have any
Windows machine. So it means that I play on Linux, using
[Steam's Proton](https://www.protondb.com), or [Wine](https://www.winehq.org)
when the game isn't natively supported on Linux.

Some games have official mod support like
[Cities: Skylines](https://skylines.paradoxwikis.com/Mods). Unfortunately you
may be quite limited to what you can mod because no extension points are
provided for everything.

Some games have unofficial, closed-source, mod loaders like, for example
[Raft](https://raft-game.com), which is using
[RaftModLoader](https://www.raftmodding.com). Unfortunately, it doesn't play
well with Linux/Proton. I failed to make it work. Some documentation do mentions
some step to make it work involving installing it on a Windows, to then copy on
the Linux. I don't find this an acceptable workaround. Documentation also
mentions that it may work better if using the compiler from a Mono installation
but that would require too much work then it won't be done. As it's
closed-source I can't contribute to solve the situation.

Some other games rely on [Harmony](https://github.com/pardeike/Harmony)
to provide mod support. This is by far the least problematic as it's a framework
to patch assemblies. The only issues I have with this it that its a bit complex
to use. Sometimes I juste want to make a minor change so I don't want to create
a full new project, dealing with dependencies, installing Harmony in the game
and all that stuff just to change two or three opcodes.

And finally, some other games or applications just don't have any mod support.
Technically, I could make it work using Harmony, as it's supposed to be working
on any .Net or Mono application. But I still finds it overkill for small
patching.

Also, I am disappointed by the [Reflexil](http://reflexil.net)'s UI/UX on
Windows and even more disappointed that there is no Linux's version or it.
The [AvaloniaILSpy](https://github.com/icsharpcode/AvaloniaILSpy) fork is
crashing very often, it's UI is full of bugs. It's a huge beast, relying on
technologies I dislike (WPF for example), so that I don't want to get involved
into it to solve my issues.

For all these reasons, I created this tool. I aim at making it as lightweight,
easy to use, portable as possible.

## Dependencies

This project depends on the following:
- [.Net Core](https://github.com/dotnet/core)
- [Mono.Cecil](https://github.com/jbevain/cecil)
- [Microsoft.CodeAnalysis](https://github.com/dotnet/roslyn)


## Platform

As this is written using .Net Core, it should run everywhere this framework is
available. Including Windows, Linux and Mac OS. Thus being said, I develop and
test only on Linux. I don't have any other OS to test, so I may not be able to
reproduce your issue.

## Build from source

```shell
git clone git@github.com:ctxnop/dotnet-patcher.git
cd dotnet-patcher
dotnet build
```

## Patching an assembly

Create a class that implement the `IPatch` interface. It may be located anywhere
in the project's folder, but you may want to keep things tidy so put it in the
`Patches` folder.

Rebuild the tool using `dotnet build`, then you can patch an assembly using the
following command:
```shell
./dotnet-patcher/out/net6.0/dp patch <assembly-to-patch>.dll <patch-id>
```

Currently, patching an assembly means using Mono.Cecil and requires
understanding the IL code which is a kind-of assembler. This can be tricky, so
you probably want to get some help from tools like ILSpy or Reflexil, to check
what was the original code, what is the patched code, how does it translate from
C# to IL and vice-versa...

```cs
public bool Apply(AssemblyDefinition asm)
{
	asm.Patch(
		(td) => {
			// This is a selection predicate: return 'true' to select this TypeDefinition
			// Called for all TypeDefinition found in the AssemblyDefinition
			return string.CompareOrdinal(td.FullName, "TypeToPatch") == 0;
		},
		(md) => {
			// This is a selection predicate: return 'true' to select this MethodDefinition
			// Called for all TypeDefinition selected by the above predicate
			return string.CompareOrdinal(md.Name, "MethodToPatch") == 0; },
		(ilp) => {
			ilp.Clear();			// Remove all previous instructions
			ilp.Emit(OpCodes.Ret);	// This method will now just return and do nothing.
		}
	);
}
```

A very early and incomplete support for C# compilation is added, so that you can
provide a method's body code. It will generate a source code corresponding to
the original but with the code you provided as the method's body. It will then
compiles everything into an in-memory assembly using Roslyn. And finally it use
Mono.Cecil back to read the resulting assembly and replace the original
intructions with the one founds in the generated assembly.

When invoking the `patch` command, a backup of the original assembly is created
in the same directory, with a `.dporg` extension. The backup is made only if not
already existed. Patches are applies on the backup file, so that calling `patch`
a second time on the same assembly won't result in a different patched assembly.
This means that you can revert any change by restoring the backup.
