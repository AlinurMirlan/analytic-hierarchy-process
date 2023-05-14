using AutoMapper;
using Fractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AHP.AutoMapper;

internal class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        base.CreateMap<string, Node>()
            .ForMember(node => node.Name, options => options.MapFrom(@string => @string));
        base.CreateMap<string, double>()
            .ConvertUsing(@string => Fraction.FromString(@string).ToDouble());
    }
}
