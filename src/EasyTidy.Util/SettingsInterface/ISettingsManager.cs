using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyTidy.Model;

namespace EasyTidy.Util.SettingsInterface;

public interface ISettingsManager
{
    public ConfigModel GetConfigModel();
}
