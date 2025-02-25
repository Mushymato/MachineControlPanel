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
    border-thickness="36, 36, 36, 36"
    *switch={PageIndex} >
    <lane *float="above" orientation="horizontal" vertical-content-alignment="end" margin="36,0,0,-24">
      <tab-label page="1" text={#rule-list.rules} margin={TabMarginRules} />
      <tab-label page="2" text={#rule-list.inputs} margin={TabMarginInputs}/>
    </lane>
    <scrollable peeking="128">
      <!-- Rules -->
      <panel *case="1">
        <grid primary-orientation="horizontal">
          <lane orientation="horizontal" *repeat={RuleEntries}>
            <rule-icon *context={:Input} />
            <image sprite={@Mods/StardewUI/Sprites/CaretRight} />
            <rule-icon *repeat={:Outputs} />
          </lane>
        </grid>
      </panel>
      <!-- Inputs -->
      <panel *case="2" >
        <grid item-layout="length: 76+" horizontal-item-alignment="middle">
          <panel *repeat={:InputItems}>
            <image sprite={:ItemData} tooltip={:Tooltip} tint={Tint}
              layout="64px 64px" 
              margin="6"
              focusable="true"
              left-click=|ToggleState()|
              +hover:scale="1.1"
              +transition:scale="100ms EaseInSine"/>
          </panel>
        </grid>
      </panel>
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
  <panel>
    <image sprite={:Sprite} tooltip={:Tooltip} layout="64px 64px" margin="6" focusable="true"/>
    <digits *if={ShowCount} number={:Count} />
  </panel>
</template>