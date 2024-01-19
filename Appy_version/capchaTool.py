import os.path

from selenium import webdriver
from selenium.webdriver.support.wait import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.common.exceptions import NoSuchElementException, TimeoutException, UnexpectedAlertPresentException
import xml.etree.ElementTree as ET
import aspose.words as aw
from time import sleep
import random
import urllib.request
import listen
import json
from datetime import datetime

# Parse the XML file
xml_file = 'setting.xml'
tree = ET.parse(xml_file)
root = tree.getroot()

variable = root.find("variable")
xpath_selector = root.find("xpath_selector")

# set up browser
options = webdriver.ChromeOptions()
options.add_argument("--headless")
options.add_argument("--window-size=1280,800")
options.add_argument("--disable-gpu")

browser = webdriver.Chrome(options=options)

track_file = variable.find("json_file").text
input_svg = variable.find("input_svg").text
output_png = variable.find("output_png").text
web_link = variable.find("web_link").text

browser.get("{}".format(web_link))


def svg_to_png():
    doc = aw.Document()
    builder = aw.DocumentBuilder(doc)
    shape = builder.insert_image(input_svg)
    pageSetup = builder.page_setup
    pageSetup.page_width = shape.width
    pageSetup.page_height = shape.height
    pageSetup.top_margin = 0
    pageSetup.left_margin = 0
    pageSetup.bottom_margin = 0
    pageSetup.right_margin = 0

    # save as PNG
    doc.save(output_png)


def delay():
    sleep(random.randint(3, 5))


def update_file(notify, creatime):
    with open("H:\\3i-Intern\\NewProjectWinForm_SmartWork\\AppNet2\\Appy_version\\Parameters.json", "r+") as f:
        data = json.load(f)
        new = dict()
        print()
        new["id"] = data[-1]["id"] + 1
        new["user"] = "B"
        new["notify"] = notify
        new["creatime"] = creatime.strftime("%Y%m%d")
        data.append(new)
        f.seek(0)
        f.truncate()
        print(new)
        json.dump(data, f, indent=2)


def slove_capcha():
    capcha = True
    with open("H:\\3i-Intern\\NewProjectWinForm_SmartWork\\AppNet2\\Appy_version\\Parameters.json", "w") as f:
        new = dict()
        new["id"] = 0
        new["user"] = "A"
        new["notify"] = ""
        new["creatime"] = ""
        f.seek(0)
        f.truncate()
        a = list()
        a.append(new)
        json.dump(a, f, indent=2)
    count = 1
    while capcha:
        try:
            src = browser.find_element(By.XPATH, xpath_selector.find("capcha_img").text).get_attribute("src")
        except NoSuchElementException:
            update_file("no_img", datetime.now())
            capcha = False
            break

        try:
            img = urllib.request.urlretrieve(src, input_svg)[0]
        except ValueError:
            img = output_png
        svg_to_png()
        update_file("img", datetime.now())

        data_input = listen.track()
        try:
            result_captcha_box = WebDriverWait(browser, 2).until(
                EC.element_to_be_clickable((By.XPATH, xpath_selector.find("capcha_box").text)))
            print(result_captcha_box)
        except TimeoutException:
            capcha = False
            break

        result_captcha_box.clear()
        result_captcha_box.send_keys(data_input.upper())
        result_captcha_box.send_keys(Keys.ENTER)
        delay()
        try:
            result = WebDriverWait(browser, 3).until(
                EC.element_to_be_clickable((By.XPATH, xpath_selector.find("title").text)))
            if result.text == "Correct!":
                update_file("correct", datetime.now())
                capcha = False
            elif count > 5:
                update_file("over", datetime.now())
                capcha = False
            else:
                print(result.text)
                update_file("fail", datetime.now())
                count += 1
        except TimeoutException:
            capcha = False


slove_capcha()
