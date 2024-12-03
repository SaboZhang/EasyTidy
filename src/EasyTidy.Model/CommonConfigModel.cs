using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public class CommonConfigModel
{
    public virtual string WebDavUrl { get; set; }

    public virtual string WebDavUser { get; set; }

    public virtual string WebDavPassword { get; set; }

    public bool? SubFolder { get; set; } = false;

    public virtual string UploadPrefix { get; set; }
}
