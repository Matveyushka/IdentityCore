const defaultTheme = {
    primaryBackground : "#009688",
    primary: "#009688",
    primaryDark: "#00796B",
    primaryLight: "#B2DFDB",
    textMain: "#212121",
    textMainFaded: "#757575",
    textContrast: "#FFFFFF",
    textContrastFaded: "#ADDFDB",
    mainBackground: "#FFFFFF",
}

const brightTheme = {
    primaryBackground: "#E91E63",
    primary: "#E91E63",
    primaryDark: "#C2185B",
    primaryLight: "#F8BBD0",
    textMain: "#212121",
    textMainFaded: "#757575",
    textContrast: "#FFFFFF",
    textContrastFaded: "#F8BCD0",
    mainBackground: "#FFDDDD",
}

const darkTheme = {
    primaryBackground: "#121212",
    primary: "#888888",
    primaryDark: "#060606",
    primaryLight: "#161616",
    textMain: "#DDDDDD",
    textMainFaded: "#CCCCCC",
    textContrast: "#DDDDDD",
    textContrastFaded: "#CCCCCC",
    mainBackground: "#242424",
}

const applyTheme = theme => {
    document.documentElement.style.setProperty('--primary-background', theme.primaryBackground)
    document.documentElement.style.setProperty('--primary', theme.primary)
    document.documentElement.style.setProperty('--primary-dark', theme.primaryDark)
    document.documentElement.style.setProperty('--primary-light', theme.primaryLight)
    document.documentElement.style.setProperty('--text-main', theme.textMain)
    document.documentElement.style.setProperty('--text-main-faded', theme.textMainFaded)
    document.documentElement.style.setProperty('--text-contrast', theme.textContrast)
    document.documentElement.style.setProperty('--text-contrast-faded', theme.textContrastFaded)
    document.documentElement.style.setProperty('--main-background', theme.mainBackground)
}

const newTheme = (name, theme) => ({
    name, theme
})

const themes = [
    newTheme("Default", defaultTheme),
    newTheme("Bright", brightTheme),
    newTheme("Dark", darkTheme)
]

const populateWithThemeButtons = (element) => {
    themes.forEach((themePair, index) => {
        const button = document.createElement('button')
        button.classList.add("btn", "btn-light", "dropdown-item")
        button.onclick = () => {
            applyTheme(themePair.theme)
            document.cookie = `theme=${index}; max-age=157680000; path=/`
            console.log(index)
        }
        button.innerText = themePair.name
        element.append(button)
    })
}

const getCookieValue = (name) => (
    document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)')?.pop() || ''
  )

applyTheme(themes[getCookieValue("theme")].theme)