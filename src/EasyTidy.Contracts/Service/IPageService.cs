using System;

namespace EasyTidy.Contracts.Service;

public interface IPageService
{
    Type GetPageType(string key);
}
