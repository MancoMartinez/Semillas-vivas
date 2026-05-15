namespace SemillasVivas.Gameplay.Demo
{
    
    public static class MobileInputState
    {
        
        public static float Horizontal { get; set; }
        
        public static float Vertical { get; set; }

        private static bool _jumpQueued;
        private static bool _attackQueued;

        public static bool JumpHeld { get; set; }

        public static void QueueJump()   => _jumpQueued   = true;
        public static void QueueAttack() => _attackQueued = true;

        public static bool ConsumeJump()
        {
            if (!_jumpQueued) return false;
            _jumpQueued = false;
            return true;
        }

        public static bool ConsumeAttack()
        {
            if (!_attackQueued) return false;
            _attackQueued = false;
            return true;
        }

        public static void Reset()
        {
            Horizontal    = 0f;
            Vertical      = 0f;
            _jumpQueued   = false;
            _attackQueued = false;
            JumpHeld      = false;
        }
    }
}
