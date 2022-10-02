# A good way to set up resource dictionaries in XAML

Microsoft has a large, complex, feature-rich system for setting up "resource dictionaries" for XAML projects. Unfortunately, their documentation doesn't provide useful guideance for small projects. This document provides that guidance


## Add AppDictionary.xaml to your project

This is the one file where all of your XAML resources will go. Here's a little sample of one of mine. It sets up a few named styles for text for a project; in particular, it's styles for little snippets of help. 
```
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style x:Key="styleHelpHeader" TargetType="TextBlock">
        <Setter Property="FontWeight" Value="Bold" />
        <Setter Property="FontSize" Value="20" />
    </Style>
    <Style x:Key="styleHelpText" TargetType="TextBlock">
        <Setter Property="FontWeight" Value="Normal" />
        <Setter Property="FontSize" Value="16" />
        <Setter Property="IsTextSelectionEnabled" Value="True" />
        <Setter Property="TextWrapping" Value="Wrap" />
    </Style>

</ResourceDictionary>
```

## Use the AppDictionary.xaml in each user control and page

You have to tell each user control and page about your snazzy new resource dictionary. Here's an example from one of my user controls

```
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="AppDictionary.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
```

## Actually use one of the styles!

And now whenever you have some help text, you can make it all consistant! Here's a little snippet from one of my apps

```
    <TextBlock Style="{StaticResource styleHelpHeader}">Frequency chart</TextBlock>
    <TextBlock Style="{StaticResource styleHelpText}">
        <Run>Shows how crowded each channel is and how the channels overlap. Full-height blocks show which channel each AP is centered on; the short blocks show what channels the each AP overlaps on. The longer the block, the stronger the signal. </Run><LineBreak />
        <Run>Hover over a block to see the name (SSID) of the associated access point (AP). Tap to show a single-line summary, and double-tab to see full details.</Run>
    </TextBlock>

```