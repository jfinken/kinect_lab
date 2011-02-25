package flash_kinect
{
    public class MovingAverage
    {
        private var _windowSize :Number;
        private var _values     :Array;
        private var _nextValIndex:Number;
        private var _sum        :Number;
        private var _valuesIn   :Number;
        
        public function MovingAverage(windowSize:Number)
        {
            _windowSize = windowSize;
            _values = new Array(_windowSize);
            
            Reset();
        }
        // Updates the moving average with its next value, and returns the updated average value.
        // When IsMature is true and NextValue is called, a previous value will 'fall out' of the
        // moving average.
        //
        // <param name="nextValue">The next value to be considered within the moving average.</param>
        // <returns>The updated moving average value.</returns>
        public function NextValue(nextVal:Number):Number
        {
            if(isNaN(nextVal))
                return 0;
            
            if(_valuesIn < _windowSize)
                _valuesIn++;
            else
                // remove oldest from sum
                _sum -= _values[_nextValIndex];
            
            // store value
            _values[_nextValIndex] = nextVal;
            
            // progress the next value index pointer
            _nextValIndex++;
            if(_nextValIndex == _windowSize)
                _nextValIndex = 0;
            
            return (_sum / _valuesIn);
        }
        // Gets a value indicating whether enough values have been provided to fill the
        // speicified window size.  Values returned from NextValue may still be used prior
        // to IsMature returning true, however such values are not subject to the intended
        // smoothing effect of the moving average's window size.
        public function IsMature():Boolean
        {
            if(_valuesIn == _windowSize)
                return true;
            else
                return false;
        }
        
        public function Reset():void
        {
            _nextValIndex = 0;
            _sum = 0;
            _valuesIn = 0;
        }
    }
}