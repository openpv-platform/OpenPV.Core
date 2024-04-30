using Ahsoka.System;
using System;
using System.Collections.Generic;

namespace Ahsoka.Storyboard;

/// <summary>
/// Type Definitions for Storyboard Metadata
/// </summary>
public interface IStoryboardMetadata : IMetadataProvider<string, string, StoryboardViewModelBase, StoryboardPropertyInfo, StoryboardMessage>
{

}