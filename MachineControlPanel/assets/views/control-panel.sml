<lane orientation="vertical" horizontal-content-alignment="middle">
  <lane orientation="horizontal" vertical-content-alignment="middle">
    <image sprite={:MachineData}
      tooltip={:MachineTooltip}
      layout="48px 96px"
      margin="12,16,12,0"
      fit="Contain"
      horizontal-alignment="middle"
      vertical-alignment="middle"
      />
    <textinput text={<>SearchText} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="300px 60px" margin="0,26,0,0" text-color="#43111B" focusable="true"/>
    <panel margin="0,26,0,0">
      <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
    </panel>
  </lane>
  <frame layout="1220px 70%[550..]"
    background={@Mods/StardewUI/Sprites/MenuBackground}
    border={@Mods/StardewUI/Sprites/MenuBorder}
    border-thickness="32, 36, 24, 36"
    *switch={PageIndex} horizontal-content-alignment="middle">
    <lane *float="above" orientation="horizontal" vertical-content-alignment="end" margin="36,0,0,-24">
      <tab-label page="1" text={#rule-list.rules} margin={TabMarginRules} />
      <tab-label page="2" text={#rule-list.inputs} margin={TabMarginInputs}/>
    </lane>
    <!-- Rules -->
    <scrollable *case="1" peeking="128">
      <grid item-layout="length: 88" padding="4,4" horizontal-item-alignment="middle"
        horizontal-divider={@Mods/StardewUI/Sprites/ThinHorizontalDivider}>
        <lane *repeat={RuleEntriesFiltered}
          pointer-enter=|~ControlPanelContext.HandleHoverRuleEntry(this)|
          pointer-leave=|~ControlPanelContext.HandleHoverRuleEntry()|
          opacity={StateOpacity}
          orientation="vertical" margin="8"
          horizontal-content-alignment="middle">
          <checkbox is-checked={<>State} margin="0,0,0,12"/>
          <rule-icon *repeat={:Input} />
          <rule-icon *repeat={:Fuel} />
          <image sprite={SpinningCaret}
            layout="36px 36px"
            margin="18,12,18,12"
            +hover:scale="1.1"
            +transition:scale="100ms EaseInSine"/>
          <rule-icon *repeat={:Outputs} />
        </lane>
      </grid>
    </scrollable>
    <!-- Inputs -->
    <scrollable *case="2" peeking="128">
      <lane orientation="vertical">
        <lane orientation="horizontal" margin="16,8">
          <image *repeat={QualityStars} sprite={:Sprite} tint={Tint}
            layout="24px 24px"
            margin="4"
            focusable="true"
            left-click=|ToggleState()|
            +hover:scale="1.1"
            +transition:scale="100ms EaseInSine"/>
        </lane>
        <image sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="stretch content" margin="0,0,8,0" fit="Stretch"/>
        <grid *case="2" item-layout="length: 76+" horizontal-item-alignment="middle">
          <panel *repeat={InputItemsFiltered}>
            <image sprite={:ItemData} tooltip={:Tooltip} tint={Tint}
              layout="64px 64px" 
              margin="6"
              focusable="true"
              left-click=|ToggleState()|
              +hover:scale="1.1"
              +transition:scale="100ms EaseInSine"/>
          </panel>
        </grid>
      </lane>
    </scrollable>
</frame>
</lane>

<template name="tab-label">
  <frame
    background={@mushymato.MachineControlPanel/sprites/cursors:tabBg}
    padding="18,16,18,12"
    focusable="true"
    margin={&margin}
    left-click=|ChangePage(&page)|
    >
    <label text={&text}/>
  </frame>
</template>

<template name="rule-icon">
  <panel padding="4" tooltip={:Tooltip} focusable="true">
    <image sprite={:Sprite} opacity={:IsMultiOpacity} layout="64px 64px" margin="2"/>
    <image *if={:IsMulti} sprite={@mushymato.MachineControlPanel/sprites/emojis:note} layout="27px 27px" />
    <panel layout="stretch stretch" horizontal-content-alignment="end" vertical-content-alignment="start">
      <image *if={:IsFuel} sprite={@mushymato.MachineControlPanel/sprites/emojis:bolt} layout="27px 27px"/>
    </panel>
    <panel *if={:HasQualityStar} layout="stretch stretch" horizontal-content-alignment="start" vertical-content-alignment="end">
      <image sprite={:QualityStar} layout="24px 24px"/>
    </panel>
    <panel *if={:ShowCount} layout="stretch stretch"  horizontal-content-alignment="end" vertical-content-alignment="end">
      <digits number={:Count} scale="3"/>
    </panel>
  </panel>
</template>