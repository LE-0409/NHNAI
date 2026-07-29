namespace NHNAI.Game.App
{
    /// <summary>
    /// 무엇으로 조작하는가. 독방 씬 위에 뜨는 메인메뉴가 고르고, 고른 값이
    /// <see cref="Player.PlayerInputSource"/> · HUD · 모바일 조작 층에 각각 전달된다.
    ///
    /// 정적 보관소를 두지 않는다. 씬이 하나뿐이라 씬을 넘어 살아야 할 값이 아니고,
    /// 정적 값은 도메인 리로드를 끈 프로젝트에서 지난 실행의 선택이 새어 나온다.
    /// 고른 값은 인자로 넘긴다 — 누가 누구에게 알려 주는지가 코드에 그대로 보인다.
    /// </summary>
    public enum ControlMode
    {
        /// <summary>키보드 · 마우스. 커서를 잠그고 논다.</summary>
        Pc,

        /// <summary>화면 위 조이스틱 · 버튼. 커서를 잠그지 않는다.</summary>
        Mobile,
    }
}
