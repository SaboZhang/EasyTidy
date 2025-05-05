using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyTidy.Model;

public class UserDefinePrompt : ICloneable
{
    public bool Enabled { get; set; }

    public string Name { get; set; }

    public List<Prompt> Prompts { get; set; }

    public UserDefinePrompt(string name, List<Prompt> prompts, bool enabled = false)
    {
        Name = name;
        Prompts = prompts;
        Enabled = enabled;
    }

    public object Clone()
    {
        return new UserDefinePrompt(Name, Prompts.Clone(), Enabled);
    }
}

public class Prompt : ICloneable
{
    public string Content { get; set; }

    public string Role { get; set; }

    public Prompt()
    {
    }

    public Prompt(string role)
    {
        Role = role;
        Content = "";
    }

    public Prompt(string role, string content)
    {
        Role = role;
        Content = content;
    }

    public object Clone()
    {
        return new Prompt(Role, Content);
    }
}

internal static class PromptExtensions
{
    public static List<T> Clone<T>(this List<T> listToClone)
        where T : Prompt
    {
        return new List<T>(listToClone.Select(item => (T)item.Clone()).ToList());
    }
}
