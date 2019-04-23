local transform;
local gameObject;

LoginPanel={};
local this=LoginPanel;

function LoginPanel.Awake(obj)
	gameObject=obj;
	transform=obj.transform;
	this.InitPanel();
	logWarn("awake lua : "..gameObject.name);
end

function LoginPanel.InitPanel()
	logWarn("LoginPanel.InitPanel");
	this.loginBtn=transform:Find("LoginBtn").gameObject;
	this.nameInput=transform:Find("NameInput"):GetComponent("InputField");
	this.paInput=transform:Find("PasswordInput"):GetComponent("InputField");
end