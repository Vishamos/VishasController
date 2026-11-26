namespace VishasController;

/// <summary>컨트롤러가 상태를 확인하는 대상(명세 02 머리말 "모듈"). Synapse 규약의 "클라이언트"와 다르다.</summary>
public enum Module
{
    Synapse,
    Body,
    Face,
    Obs,
    Chzzk,
    Vishamos,
}

public static class ModuleExtensions
{
    /// <summary>화면에 쓰는 모듈 이름.</summary>
    public static string DisplayName(this Module module) => module switch
    {
        Module.Synapse => "시냅스",
        Module.Body => "바디",
        Module.Face => "페이스",
        Module.Obs => "OBS",
        Module.Chzzk => "치지직",
        Module.Vishamos => "비샤모스",
        _ => module.ToString(),
    };
}
