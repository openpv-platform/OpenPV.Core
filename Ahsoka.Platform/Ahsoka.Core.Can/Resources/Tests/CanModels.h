enum TestEnumValues
{
    Test2EnumOne = 1,
    Test2EnumTwo = 2
};

class TestCanModel : public CanViewModelBase
{
    public:
        
        enum Properties
        {
            TestEnum,
            TestUnsigned,
            TestSigned,
            TestFloat,
            TestDouble
        };

        TestCanModel() : CanViewModelBase(500, 16) { } // CANID: 0x1f4
        TestCanModel(CanMessageData message) : CanViewModelBase(message) { }

        TestEnumValues GetTestEnum() { return OnGetValue<TestEnumValues>(TestEnum); }
        void SetTestEnum(TestEnumValues value) { OnSetValue<TestEnumValues>(value, TestEnum); }

        uint GetTestUnsigned() { return OnGetValue<uint>(TestUnsigned); }
        void SetTestUnsigned(uint value) { OnSetValue<uint>(value, TestUnsigned); }

        int GetTestSigned() { return OnGetValue<int>(TestSigned); }
        void SetTestSigned(int value) { OnSetValue<int>(value, TestSigned); }

        uint GetTestFloat() { return OnGetValue<float>(TestFloat); }
        void SetTestFloat(float value) { OnSetValue<float>(value, TestFloat); }

        uint GetTestDouble() { return OnGetValue<double>(TestDouble); }
        void SetTestDouble(double value) { OnSetValue<double>(value, TestDouble); }

    protected:
    
        std::map<int, CanPropertyInfo>& GetMetadata();
};


class CanModelMetadata : public CanViewModelBase
{
    public:
       
        static std::map<int, CanPropertyInfo>& GetMetadata(uint canID) 
        { 
            if (metadata.size() == 0)
                LoadMetadata(); 

            return metadata[canID]; 
        }

        static CanPropertyInfo& GetPropertyInfo(uint canID, uint signal, bool isSignalEnum) 
        { 
            static CanPropertyInfo emptyInfo;
     
            if (metadata.size() == 0)
                LoadMetadata(); 

            if (isSignalEnum)
                return metadata[canID][signal]; 
            else
            {
                for(auto& it : metadata[canID])
                    if (it.second.GetSignalId() == signal)
                        return it.second;
                
                return emptyInfo;
            }
        }
   
    protected:
    
        static std::map<uint,std::map<int, CanPropertyInfo>> metadata;

        static void LoadMetadata()
        {
            // Add Props for TestCanModel - CANID: 500
            std::map<int, CanPropertyInfo> testCanModelProps;
            testCanModelProps[TestCanModel::Properties::TestEnum] = CanPropertyInfo(4, 8, ByteOrder::LittleEndian, ValueType::Enum, 1, 0, 10);
            testCanModelProps[TestCanModel::Properties::TestUnsigned] = CanPropertyInfo(12, 8, ByteOrder::LittleEndian, ValueType::Unsigned, 2, 0, 11);
            testCanModelProps[TestCanModel::Properties::TestSigned] = CanPropertyInfo(22, 8, ByteOrder::LittleEndian, ValueType::Signed, 1, 0, 12);
            testCanModelProps[TestCanModel::Properties::TestFloat] = CanPropertyInfo(32, 32, ByteOrder::LittleEndian, ValueType::Float, 1, 0, 13);
            testCanModelProps[TestCanModel::Properties::TestDouble] = CanPropertyInfo(64, 64, ByteOrder::LittleEndian, ValueType::Double, 1, 0, 14);
            metadata[500] = testCanModelProps;
        }
       
};

// Implementations
 std::map<uint,std::map<int, CanPropertyInfo>> CanModelMetadata::metadata;
 std::map<int, CanPropertyInfo>& TestCanModel::GetMetadata() { return CanModelMetadata::GetMetadata(500); }