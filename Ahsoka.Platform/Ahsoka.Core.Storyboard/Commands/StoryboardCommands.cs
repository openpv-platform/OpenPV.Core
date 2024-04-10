using Ahsoka.Storyboard;
using Ahsoka.System;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class StoryboardCommands
{
    [CommandLineMethod(@"     --GenerateStoryboardClasses: Generate Classes for a Storyboard Project
            [PathToSource] Path to Storyboard CustomEvents.evt file
            [PathToDestinationFolder] Folder where Class Definitions will be created
            [NameSpace] NameSpace for Output Classes
            [BaseClass] Base Class (or View Model) for Output Classes")]
    private static void GenerateStoryboardClasses(string pathToSource, string pathToDestinationFolder, string nameSpace, string baseClass)
    {
        StoryboardTools.GenerateMessageClasses(pathToSource, pathToDestinationFolder, nameSpace, baseClass);
    }
}